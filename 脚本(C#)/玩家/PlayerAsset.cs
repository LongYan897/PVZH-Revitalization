using Godot;
using System;
using Variable;

namespace Play;
public partial class PlayerAsset
{
    /// <summary>
    /// 玩家的幸运值，影响游戏中随机事件的概率<br/>
    /// </summary>
    public Variable<double> Lucky { get; private set; } = new Variable<double>("Lucky", 0.0);
}
