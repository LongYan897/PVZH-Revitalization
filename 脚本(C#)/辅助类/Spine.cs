using Godot;
using System;

namespace Pack;

public class SpineSpriteWrapper
{
    public readonly Node Target;

    public SpineSpriteWrapper(Node target)
    {
        Target = target;
    }

    public GodotObject GetSkeleton()
    {
        return Target.Call("GetSkeleton").As<GodotObject>();
    }

    public void SetSkeletonData(Resource skeletonDataResource)
    {
        Target.Call("SetSkeletonData", skeletonDataResource);
    }

    public Resource LoadSkeletonDataFromFiles(string skelPath, string atlasPath)
    {
        GodotObject skelFileRes = new GodotObject();
        skelFileRes.Call("new");
        var err = skelFileRes.Call("load_from_file", skelPath).AsInt32();
        if (err != 0)
        {
            GD.PrintErr("skel load error: " + skelPath);
            return null;
        }

        GodotObject atlasRes = new GodotObject();
        atlasRes.Call("new");
        err = atlasRes.Call("load_from_atlas_file", atlasPath).AsInt32();
        if (err != 0)
        {
            GD.PrintErr("atlas load error: " + atlasPath);
            return null;
        }

        GodotObject skeletonDataRes = new GodotObject();
        skeletonDataRes.Call("new");
        skeletonDataRes.Set("skeleton_file", skelFileRes);
        skeletonDataRes.Set("atlas", atlasRes);
        return skeletonDataRes as Resource;
    }

    public void SetAnimation(int track, string animName, bool loop)
    {
        GodotObject animState = Target.Call("GetAnimationState").As<GodotObject>();
        animState.Call("SetAnimation", track, animName, loop);
    }
    public void AddAnimation(int track, string animName, bool loop, float delay = 0)
    {
        GodotObject animState = Target.Call("GetAnimationState").As<GodotObject>();
        animState.Call("AddAnimation", track, animName, loop, delay);
    }

    public Transform2D GetGlobalBoneTransform(string boneName)
    {
        return Target.Call("GetGlobalBoneTransform", boneName).AsTransform2D();
    }
    public Vector2 GetBoneWorldPos(string boneName)
    {
        return GetGlobalBoneTransform(boneName).Origin;
    }

    public event Action<GodotObject> AnimationEvent;
    public void ConnectEvent()
    {
        Target.Connect("animation_event", Callable.From<GodotObject>(OnEvent));
    }
    private void OnEvent(GodotObject evt)
    {
        AnimationEvent?.Invoke(evt);
    }
}

public class SpineSkeletonWrapper
{
    private readonly GodotObject _native;
    public SpineSkeletonWrapper(GodotObject native) => _native = native;

    public SpineBoneWrapper FindBone(string name)
    {
        var obj = _native.Call("FindBone", name).As<GodotObject>();
        return obj == null ? null : new SpineBoneWrapper(obj);
    }
    public SpineSlotWrapper FindSlot(string name)
    {
        var obj = _native.Call("FindSlot", name).As<GodotObject>();
        return obj == null ? null : new SpineSlotWrapper(obj);
    }
    public void SetSkin(string skinName)
    {
        _native.Call("SetSkin", skinName);
        _native.Call("SetBonesToSetupPose");
    }
}

public class SpineBoneWrapper
{
    private readonly GodotObject _native;
    public SpineBoneWrapper(GodotObject native) => _native = native;
    public Vector2 WorldPos => _native.Call("GetWorldPosition").AsVector2();
    public float WorldRot => _native.Call("GetWorldRotation").AsSingle();
}

public class SpineSlotWrapper
{
    private readonly GodotObject _native;
    public SpineSlotWrapper(GodotObject native) => _native = native;
    public Color Color => _native.Get("Color").AsColor();
}
