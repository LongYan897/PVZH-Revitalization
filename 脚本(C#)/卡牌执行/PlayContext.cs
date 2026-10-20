using Battle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Target;

/// <summary>
/// 卡牌打出对象的包装类
/// </summary>
public struct PlayContext
{
    public static PlayContext Play(
        ITarget target,
        TargetType targetType = TargetType.Grids,
        RoadTargetKind roadTargetKind = RoadTargetKind.None
        )
    {
        if (target is not NodeRoad)
            roadTargetKind = RoadTargetKind.None;
        return new PlayContext
        {
            Target = target,
            RoadKind = roadTargetKind,
            TargetType = targetType,
        };
    }
    public ITarget Target {  get; private set; }
    public RoadTargetKind RoadKind { get; private set; }
    public TargetType TargetType { get; private set; }
}
