using Godot;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controller;

[GlobalClass]
public partial class GemCenter : Panel
{
    private static GemCenter Instance;
    public override void _EnterTree()
    {
        Instance = this;
    }
    public override void _ExitTree()
    {
        Instance = null;
    }
    public static void GainGem(int amount)
    {
        Instance.GetNode<DotLabel>("钻石数量").Amount += amount;
    }
    public static void LoseGem(int amount)
    {
        Instance.GetNode<DotLabel>("钻石数量").Amount -= amount;
    }
}
