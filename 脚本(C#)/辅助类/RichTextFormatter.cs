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

    private enum ParamKind { None, String, Int, Bool }

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

    // 修复：禁止跨越 } 和 +，防止贪婪吃掉普通文本
    private static readonly Regex PlaceholderRegex =
        new Regex(@"\{([#&/]+):([^}|+]+)(?:\+([^}|]+))?(?:\|([^}]*))?\}", RegexOptions.Compiled);

    private static readonly Regex UnderlineRegex =
        new Regex(@"~([^~]+)~", RegexOptions.Compiled);

    public static string ConvertUnderlines(string text) =>
        UnderlineRegex.Replace(text, mm => $"[u]{mm.Groups[1].Value}[/u]");

    public static string ConvertLabels(string text) =>
        LabelRegex.Replace(text,
            mm => $"[color=#b3ff30][u]{mm.Groups[1].Value}[/u][/color]");

    public static string ConvertLinks(string text)
    {
        text = ConvertUnderlines(text);

        text = ArgBeforeRegex.Replace(text, mm =>
        {
            char sep = mm.Groups[1].Value[0];
            string arg = mm.Groups[2].Value;
            string word = mm.Groups[3].Value;
            bool isNullArg = arg == "null";
            if (isNullArg) arg = null;
            string meta = $"{word}|{arg}|{sep}|before";
            // 修复：内部如果带#，转为绿色标签
            string showText = isNullArg ? word : (sep == '#' ? $"{ConvertLabels(arg)}{word}" : $"{arg}{word}");
            return $"[color=#44ffff][url={meta}]{showText}[/url][/color]";
        });

        text = ArgAfterRegex.Replace(text, mm =>
        {
            string word = mm.Groups[1].Value;
            char sep = mm.Groups[2].Value[0];
            string arg = mm.Groups[3].Value;
            bool isNullArg = arg == "null";
            if (isNullArg) arg = null;
            string meta = $"{word}|{arg}|{sep}|after";
            // 修复：内部如果带#，转为绿色标签
            string showText = isNullArg ? word : (sep == '#' ? $"{word}{ConvertLabels(arg)}" : $"{word}{arg}");
            return $"[color=#44ffff][url={meta}]{showText}[/url][/color]";
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
            bool isNullArg = arg == "null";
            if (isNullArg) arg = null;
            meta = $"{word}|{arg}|{sep}|before";
            display = isNullArg ? word : (sep == '#' ? $"${arg}${word}" : $"{arg}{word}");
            return true;
        }

        var ma = ArgAfterRegex.Match(raw);
        if (ma.Success)
        {
            string word = ma.Groups[1].Value;
            char sep = ma.Groups[2].Value[0];
            string arg = ma.Groups[3].Value;
            bool isNullArg = arg == "null";
            if (isNullArg) arg = null;
            meta = $"{word}|{arg}|{sep}|after";
            display = isNullArg ? word : (sep == '#' ? $"{word}${arg}$" : $"{word}{arg}");
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

    private static Dictionary<string, (MethodInfo method, bool acceptEffects, ParamKind kind1, ParamKind kind2)> _urlParamsCache;
    private static readonly object _urlParamsCacheLock = new();

    private static bool TryMapKind(Type t, out ParamKind kind)
    {
        if (t == typeof(string)) { kind = ParamKind.String; return true; }
        if (t == typeof(int)) { kind = ParamKind.Int; return true; }
        if (t == typeof(bool)) { kind = ParamKind.Bool; return true; }
        kind = ParamKind.None;
        return false;
    }

    private static Dictionary<string, (MethodInfo method, bool acceptEffects, ParamKind kind1, ParamKind kind2)> GetUrlParamsCache()
    {
        if (_urlParamsCache != null) return _urlParamsCache;

        lock (_urlParamsCacheLock)
        {
            if (_urlParamsCache != null) return _urlParamsCache;

            var flags = BindingFlags.Public | BindingFlags.NonPublic
                      | BindingFlags.Static | BindingFlags.FlattenHierarchy;

            var cache = new Dictionary<string, (MethodInfo, bool, ParamKind, ParamKind)>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in typeof(RichTextFormatter).Assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(flags))
                {
                    if (!Attribute.IsDefined(method, typeof(UrlParamsAttribute))) continue;
                    if (method.ReturnType != typeof(string)) continue;
                    if (!method.IsStatic) continue;

                    var ps = method.GetParameters();
                    if (ps.Length < 1 || ps.Length > 3) continue;
                    if (ps[0].ParameterType != typeof(CardModel)) continue;

                    ParamKind kind1 = ParamKind.None, kind2 = ParamKind.None;
                    if (ps.Length >= 2 && !TryMapKind(ps[1].ParameterType, out kind1)) continue;
                    if (ps.Length == 3 && !TryMapKind(ps[2].ParameterType, out kind2)) continue;

                    if (cache.ContainsKey(method.Name)) continue;

                    var attr = (UrlParamsAttribute)Attribute.GetCustomAttribute(
                        method, typeof(UrlParamsAttribute));
                    cache[method.Name] = (method, attr.AcceptEffects, kind1, kind2);
                }
            }

            _urlParamsCache = cache;
            return _urlParamsCache;
        }
    }

    private static bool TryParseBool(string s, out bool value)
    {
        if (bool.TryParse(s, out value)) return true;
        switch (s.Trim())
        {
            case "1": case "是": case "真": value = true; return true;
            case "0": case "否": case "假": value = false; return true;
        }
        value = false;
        return false;
    }

    private static bool TryConvertArg(string s, ParamKind kind, out object value)
    {
        value = null;

        if (kind == ParamKind.String)
        {
            value = s ?? "";
            return true;
        }

        if (string.IsNullOrWhiteSpace(s)) return false;

        switch (kind)
        {
            case ParamKind.Int:
                if (int.TryParse(s.Trim(), out int i)) { value = i; return true; }
                return false;
            case ParamKind.Bool:
                if (TryParseBool(s, out bool b)) { value = b; return true; }
                return false;
            default:
                return false;
        }
    }

    private static bool TrySplitTwoArgs(string arg, out string a, out string b)
    {
        a = b = null;
        if (string.IsNullOrWhiteSpace(arg)) return false;
        int idx = arg.IndexOf(',');
        if (idx < 0) return false;
        a = arg.Substring(0, idx).Trim();
        b = arg.Substring(idx + 1).Trim();
        return a.Length > 0 && b.Length > 0;
    }

    private static (string value, bool acceptEffects) GetUrlParamValue(string key, CardModel model, string arg)
    {
        if (string.IsNullOrEmpty(key) || key == "n" || key == "x")
            return (null, true);

        // 修复：自动处理掉误写的圆括号
        string cleanKey = key.Trim().Trim('(', ')');

        var cache = GetUrlParamsCache();
        if (!cache.TryGetValue(cleanKey, out var entry))
            return (null, true);

        bool hasArg = !string.IsNullOrWhiteSpace(arg);

        try
        {
            if (entry.kind1 == ParamKind.None)
            {
                if (model == null) return (null, entry.acceptEffects);
                var v0 = entry.method.Invoke(null, new object[] { model }) as string;
                return (v0, entry.acceptEffects);
            }

            if (entry.kind2 == ParamKind.None)
            {
                if (!hasArg && entry.kind1 != ParamKind.String) return (null, entry.acceptEffects);
                if (!TryConvertArg(arg, entry.kind1, out object a1))
                    return (null, entry.acceptEffects);
                if (model == null) return (null, entry.acceptEffects);

                var v1 = entry.method.Invoke(null, new object[] { model, a1 }) as string;
                return (v1, entry.acceptEffects);
            }

            if (!hasArg) return (null, entry.acceptEffects);
            if (!TrySplitTwoArgs(arg, out string s1, out string s2))
                return (null, entry.acceptEffects);
            if (!TryConvertArg(s1, entry.kind1, out object a2))
                return (null, entry.acceptEffects);
            if (!TryConvertArg(s2, entry.kind2, out object a3))
                return (null, entry.acceptEffects);
            if (model == null) return (null, entry.acceptEffects);

            var v2 = entry.method.Invoke(null, new object[] { model, a2, a3 }) as string;
            return (v2, entry.acceptEffects);
        }
        catch
        {
            return (null, entry.acceptEffects);
        }
    }

    public static string ReplacePlaceholders(string rawDesc, string arg, char sep, CardModel model)
    {
        if (string.IsNullOrEmpty(rawDesc)) return rawDesc;

        return PlaceholderRegex.Replace(rawDesc, m =>
        {
            string allowed = m.Groups[1].Value;
            string left = m.Groups[2].Value;
            bool hasPlus = m.Groups[3].Success;
            string right = m.Groups[3].Value;

            // 修复：如果没写 |fallback，失败时保留原样，不再乱删字
            string fallback = m.Groups[4].Success ? m.Groups[4].Value : m.Value;

            string EvalSingle(string key, string inArg, out bool ok)
            {
                ok = false;
                if (string.IsNullOrWhiteSpace(inArg)) inArg = null;

                char workingSep = sep;
                if (workingSep == '\0')
                {
                    if (allowed.Contains('#')) workingSep = '#';
                    else if (allowed.Contains('&')) workingSep = '&';
                    else return null;
                }
                if (!allowed.Contains(workingSep)) return null;

                if (key == "n" || key == "x")
                {
                    ok = true;
                    return inArg ?? "";
                }

                if (model == null) return null;

                var (v, acceptEffects) = GetUrlParamValue(key, model, inArg);
                if (v == null) return null;
                ok = true;
                if (!acceptEffects) return v;
                return workingSep == '#' ? $"${v}$" : v;
            }

            string valL = EvalSingle(left, arg, out bool lOk);
            if (!lOk) return fallback;
            if (!hasPlus) return valL;
            string valR = EvalSingle(right, valL, out bool rOk);
            return rOk ? valR : fallback;
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