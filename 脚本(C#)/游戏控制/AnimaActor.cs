using Godot;
using Spine;
using System.Threading.Tasks;

namespace Controller;

/// <summary>
/// 动画播放器(播放全局坐标的动画)
/// </summary>
public static class AnimaActor
{
    public static async Task PlayInstantAnimation(string skelPath, Vector2 pos, string name)
    {
        var sp = SpineHandler.Get();
        sp.Position = pos;
        Main.Animator.AddChild(sp);
        sp.LoadSkeletonData(skelPath);
        await sp.SetAnimationAndFreeOnEndTask(0, name);
    }
}
