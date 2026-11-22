using Godot;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest;

[GlobalClass]
public partial class NodeQuest : Control
{
    private static readonly PackedScene Scene = "res://场景(C#)/任务便签.tscn".Load<PackedScene>();
}
