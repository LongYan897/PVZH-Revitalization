using Godot;
using System.Collections.Generic;

namespace Pack;

public static class StringExtension
{
    private static readonly Dictionary<string, GodotObject> _cache = new();

    public static T Load<T>(this string path) where T : GodotObject
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (_cache.TryGetValue(path, out var cached) && cached is T typed && GodotObject.IsInstanceValid(typed))
            return typed;

        var res = GD.Load<T>(path);
        if (res != null)
            _cache[path] = res;
        return res;
    }
}