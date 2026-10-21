using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pack;

public static class StringExtension
{
    public static T Load<T>(this string path) where T : GodotObject
    {
        return GD.Load<T>(path);
    }
}
