using Card;
using Controller;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pack;

public static class RichTextFormatter
{
    public const int IconSize = 36;

    private static readonly Regex IconRegex =
        new Regex(@"=([^=#\s]+)(?:#([^#=\s]+)#)?=(?:\{([+-]?\d+)\})?", RegexOptions.Compiled);

    private static readonly Regex UrlIconRegex =
        new Regex(@"==([^=]+?)(?:\{([^}]*)\})?==", RegexOptions.Compiled);

    private static readonly Regex LinkRegex =
        new Regex(@"_([^_]+)_", RegexOptions.Compiled);

    private static readonly Regex LabelRegex =
        new Regex(@"\$([^$]+)\$", RegexOptions.Compiled);

    private static readonly Regex ArgBeforeRegex =
        new Regex(@"_([&#])([^&#_]+)\1([^_]+)_", RegexOptions.Compiled);

    private static readonly Regex ArgAfterRegex =
        new Regex(@"_([^&#_]+)([&#])([^&#_]+)\2_", RegexOptions.Compiled);

    private static readonly Regex PlaceholderRegex =
        new Regex(@"\{([#&/]+):(\w+)\}", RegexOptions.Compiled);

    public static string ConvertLabels(string text) =>
        LabelRegex.Replace(text,
            mm => $"[color=#b3ff30][u]{mm.Groups[1].Value}[/u][/color]");

    public static string ConvertLinks(string text)
    {
        text = ArgBeforeRegex.Replace(text, mm =>
        {
            char sep = mm.Groups[1].Value[0];
            string arg = mm.Groups[2].Value;
            string word = mm.Groups[3].Value;
            string meta = $"{word}|{arg}|{sep}|before";
            return $"[color=#44ffff][url={meta}]{arg}{word}[/url][/color]";
        });

        text = ArgAfterRegex.Replace(text, mm =>
        {
            string word = mm.Groups[1].Value;
            char sep = mm.Groups[2].Value[0];
            string arg = mm.Groups[3].Value;
            string meta = $"{word}|{arg}|{sep}|after";
            return $"[color=#44ffff][url={meta}]{word}{arg}[/url][/color]";
        });

        text = LinkRegex.Replace(text,
            mm => $"[color=#44ffff][url={mm.Groups[1].Value}]{mm.Groups[1].Value}[/url][/color]");

        return text;
    }

    public static bool TryExtractLinkMeta(string raw, out string meta, out string display)
    {
        meta = null;
        display = null;

        var mb = ArgBeforeRegex.Match(raw);
        if (mb.Success)
        {
            char sep = mb.Groups[1].Value[0];
            string arg = mb.Groups[2].Value;
            string word = mb.Groups[3].Value;
            meta = $"{word}|{arg}|{sep}|before";
            display = sep == '#' ? $"${arg}${word}" : $"{arg}{word}";
            return true;
        }

        var ma = ArgAfterRegex.Match(raw);
        if (ma.Success)
        {
            string word = ma.Groups[1].Value;
            char sep = ma.Groups[2].Value[0];
            string arg = ma.Groups[3].Value;
            meta = $"{word}|{arg}|{sep}|after";
            display = sep == '#' ? $"{word}${arg}$" : $"{word}{arg}";
            return true;
        }

        var ml = LinkRegex.Match(raw);
        if (ml.Success)
        {
            meta = ml.Groups[1].Value;
            display = ml.Groups[1].Value;
            return true;
        }

        return false;
    }

    private static Dictionary<string, (MethodInfo method, bool acceptEffects)> _urlParamsCache;
    private static readonly object _urlParamsCacheLock = new();

    private static Dictionary<string, (MethodInfo method, bool acceptEffects)> GetUrlParamsCache()
    {
        if (_urlParamsCache != null) return _urlParamsCache;

        lock (_urlParamsCacheLock)
        {
            if (_urlParamsCache != null) return _urlParamsCache;

            var flags = BindingFlags.Public | BindingFlags.NonPublic
                      | BindingFlags.Static | BindingFlags.FlattenHierarchy;

            var cache = new Dictionary<string, (MethodInfo, bool)>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in typeof(RichTextFormatter).Assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(flags))
                {
                    if (!Attribute.IsDefined(method, typeof(UrlParamsAttribute))) continue;
                    if (method.ReturnType != typeof(string)) continue;
                    if (!method.IsStatic) continue;

                    var ps = method.GetParameters();
                    if (ps.Length != 1 || ps[0].ParameterType != typeof(CardModel)) continue;

                    if (cache.ContainsKey(method.Name)) continue;

                    var attr = (UrlParamsAttribute)Attribute.GetCustomAttribute(
                        method, typeof(UrlParamsAttribute));
                    cache[method.Name] = (method, attr.AcceptEffects);
                }
            }

            _urlParamsCache = cache;
            return _urlParamsCache;
        }
    }

    private static (string value, bool acceptEffects) GetUrlParamValue(string key, CardModel model)
    {
        if (string.IsNullOrEmpty(key) || key == "n" || key == "x")
            return (null, true);

        var cache = GetUrlParamsCache();
        if (!cache.TryGetValue(key, out var entry))
            return (null, true);

        var v = entry.method.Invoke(null, new object[] { model }) as string;
        return (v, entry.acceptEffects);
    }

    public static string ReplacePlaceholders(string rawDesc, string arg, char sep, CardModel model)
    {
        if (string.IsNullOrEmpty(rawDesc)) return rawDesc;

        return PlaceholderRegex.Replace(rawDesc, m =>
        {
            string allowed = m.Groups[1].Value;
            string kind = m.Groups[2].Value;

            if (kind == "n")
            {
                if (arg == null || !allowed.Contains(sep)) return m.Value;
                return sep == '#' ? $"${arg}$" : arg;
            }

            if (kind == "x")
            {
                if (arg == null || !allowed.Contains(sep)) return m.Value;
                return arg;
            }

            var (custom, acceptEffects) = GetUrlParamValue(kind, model);
            if (custom == null) return m.Value;

            char useSep = sep;
            if (useSep == '\0')
            {
                if (allowed.Contains('#')) useSep = '#';
                else if (allowed.Contains('&')) useSep = '&';
                else return m.Value;
            }
            else if (!allowed.Contains(useSep))
            {
                return m.Value;
            }

            if (!acceptEffects) return custom;

            return useSep == '#' ? $"${custom}$" : custom;
        });
    }

    private static Dictionary<string, PropertyInfo> _urlCache;
    private static readonly object _urlCacheLock = new();

    private static Dictionary<string, PropertyInfo> GetURLCache()
    {
        if (_urlCache != null) return _urlCache;

        lock (_urlCacheLock)
        {
            if (_urlCache != null) return _urlCache;

            var flags = BindingFlags.Public
                      | BindingFlags.NonPublic
                      | BindingFlags.Static
                      | BindingFlags.Instance
                      | BindingFlags.FlattenHierarchy;

            var cache = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in typeof(RichTextFormatter).Assembly.GetTypes())
            {
                foreach (var prop in type.GetProperties(flags))
                {
                    if (!Attribute.IsDefined(prop, typeof(URLAttribute))) continue;
                    if (prop.PropertyType != typeof(string)) continue;

                    var getter = prop.GetGetMethod(true);
                    if (getter == null) continue;
                    if (!getter.IsStatic) continue;

                    if (!cache.ContainsKey(prop.Name))
                        cache[prop.Name] = prop;
                }
            }

            _urlCache = cache;
            return _urlCache;
        }
    }

    public static bool TryGetKeywordDescription(
        CardModel model, string key, string arg, char sep, out string desc)
    {
        desc = null;
        if (string.IsNullOrEmpty(key)) return false;

        if (model != null)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic
                      | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase;
            var prop = model.GetType().GetProperty(key, flags);
            if (TryGetFromProp(prop, model, out string rawDesc))
            {
                desc = ReplacePlaceholders(rawDesc, arg, sep, model);
                return true;
            }
        }

        var cache = GetURLCache();
        if (cache.TryGetValue(key, out var staticProp) &&
            TryGetFromProp(staticProp, null, out string staticDesc))
        {
            desc = ReplacePlaceholders(staticDesc, arg, sep, model);
            return true;
        }

        return false;
    }

    private static bool TryGetFromProp(PropertyInfo prop, object instance, out string desc)
    {
        desc = null;
        if (prop == null) return false;
        if (!Attribute.IsDefined(prop, typeof(URLAttribute))) return false;
        if (prop.PropertyType != typeof(string)) return false;

        var getter = prop.GetGetMethod(true);
        if (getter == null) return false;

        object target = getter.IsStatic ? null : instance;
        if (!getter.IsStatic && target == null) return false;

        desc = prop.GetValue(target) as string;
        return !string.IsNullOrEmpty(desc);
    }

    public static async Task AppendAsync(
        RichTextLabel label,
        string raw,
        bool convertLinks = true,
        float extraSize = 0f,
        Func<bool> isCancelled = null,
        CardModel model = null,
        string placeholderArg = null,
        char placeholderSep = '\0')
    {
        if (label == null) return;
        if (isCancelled != null && isCancelled()) return;

        label.BbcodeEnabled = true;
        label.Clear();
        if (string.IsNullOrEmpty(raw)) return;

        if (model != null)
            raw = ReplacePlaceholders(raw, placeholderArg, placeholderSep, model);

        var composer = IconComposer.EnsureInstance();
        if (composer == null) return;

        var all = new List<(Match m, bool isUrl)>();
        foreach (Match m in UrlIconRegex.Matches(raw)) all.Add((m, true));
        foreach (Match m in IconRegex.Matches(raw))
        {
            if (all.Any(t => t.isUrl && m.Index >= t.m.Index && m.Index + m.Length <= t.m.Index + t.m.Length))
                continue;
            all.Add((m, false));
        }
        all.Sort((a, b) => a.m.Index.CompareTo(b.m.Index));

        int last = 0;
        foreach (var (m, isUrl) in all)
        {
            if (isCancelled != null && isCancelled()) return;
            if (m.Index < last) continue;

            if (m.Index > last)
            {
                string seg = raw.Substring(last, m.Index - last);
                seg = ConvertLabels(seg);
                label.AppendText(convertLinks ? ConvertLinks(seg) : seg);
            }

            if (isUrl)
            {
                string name = m.Groups[1].Value.Trim();
                string urlRaw = m.Groups[2].Success ? m.Groups[2].Value : null;

                if (Icon.TryGet(name, out var path, out var attr))
                {
                    if (!string.IsNullOrEmpty(urlRaw) && TryExtractLinkMeta(urlRaw, out string meta, out string display))
                    {
                        string inner = $"[img={IconSize + extraSize}]{path}[/img]{ConvertLabels(display)}";
                        label.AppendText($"[color=#44ffff][url={meta}]{inner}[/url][/color]");
                    }
                    else
                    {
                        string meta2 = string.IsNullOrEmpty(urlRaw) ? name : urlRaw;
                        bool hasNum = int.TryParse(meta2, out _);

                        if (hasNum)
                        {
                            var tex = await composer.ComposeAsync(
                                path, meta2, attr.Color, attr.OutlineColor, attr.OutlineSize);
                            if (isCancelled != null && isCancelled()) return;
                            if (tex != null)
                            {
                                float scale = (IconSize + extraSize) / tex.GetWidth();
                                label.AddImage(tex, tex.GetWidth() * scale, tex.GetHeight() * scale);
                            }
                        }
                        else
                        {
                            label.AppendText($"[color=#44ffff][url={meta2}][img={IconSize + extraSize}]{path}[/img][/url][/color]");
                        }
                    }
                }
                else
                {
                    label.AppendText(m.Value);
                }
            }
            else
            {
                string name = m.Groups[1].Value;
                bool hasNum = m.Groups[3].Success;
                string value = hasNum ? m.Groups[3].Value : null;

                if (Icon.TryGet(name, out var path, out var attr))
                {
                    if (hasNum)
                    {
                        var tex = await composer.ComposeAsync(
                            path, value, attr.Color, attr.OutlineColor, attr.OutlineSize);
                        if (isCancelled != null && isCancelled()) return;
                        if (tex != null)
                        {
                            float scale = (IconSize + extraSize) / tex.GetWidth();
                            label.AddImage(tex, tex.GetWidth() * scale, tex.GetHeight() * scale);
                        }
                    }
                    else
                    {
                        label.AppendText($"[img={IconSize + extraSize}]{path}[/img]");
                    }
                }
            }

            last = m.Index + m.Length;
        }

        if (isCancelled != null && isCancelled()) return;

        if (last < raw.Length)
        {
            string seg = raw.Substring(last);
            seg = ConvertLabels(seg);
            label.AppendText(convertLinks ? ConvertLinks(seg) : seg);
        }
    }
}