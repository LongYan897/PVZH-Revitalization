using Godot;
using System;
using System.Diagnostics;

namespace Spine;

[GlobalClass]
///<summary>
///用于辅助Spine动画的类
/// </summary>
public partial class SpineHandler : Node2D
{
	private Node _spineSprite;

	public override void _Ready()
	{
		_spineSprite = GetNode<Node>("Spine");
	}

	/// <summary>
	/// 加载 SpineSkeletonDataResource (.tres)
	/// </summary>
	public void LoadSkeletonData(string tresPath)
	{
		if (_spineSprite == null)
		{
			return;
		}
		Resource data = ResourceLoader.Load(tresPath);
		if (data == null)
		{
			return;
		}
		_spineSprite.Set("skeleton_data", data);
	}

	public void SetAnimation(int track, string animName, bool loop)
	{
		_spineSprite.Call("set_animation", track, animName, loop);
	}

	public void AddAnimation(int track, string animName, bool loop, float delay = 0.0f)
	{
		_spineSprite.Call("add_animation", track, animName, loop, delay);
	}

	public void SetSkin(string skinName)
	{
		_spineSprite.Call("set_skin", skinName);
	}

	public Vector2 GetBoneWorldPos(string boneName)
	{
		return _spineSprite.Call("get_bone_world_pos", boneName).AsVector2();
	}

	public event Action<GodotObject> OnSpineAnimEvent;

	public void ConnectAnimationEvent()
	{
		_spineSprite.Connect("spine_anim_event", Callable.From<GodotObject>(evt =>
		{
			OnSpineAnimEvent?.Invoke(evt);
		}));
	}
    public void ClearTrack(int track)
    {
        if (_spineSprite == null) return;
        _spineSprite.Call("clear_track",track);
    }
	public void ClearTracks()
	{
        if (_spineSprite == null) return;
        _spineSprite.Call("clear_tracks");
    }
}
