using Godot;
using System;
using System.Threading.Tasks;

namespace Spine;

[GlobalClass]
///<summary>
///用于辅助Spine动画的类
/// </summary>
public partial class SpineHandler : Node2D
{
	private static PackedScene Scene => GD.Load<PackedScene>("res://场景(C#)/动画.tscn");
	private Node _spineSprite;

	private Node SpineSprite
	{
		get
		{
			if (_spineSprite == null)
			{
				_spineSprite = GetNode<Node>("SpineNode");
				ConnectAnimationEvent();
			}
			return _spineSprite;
		}
	}
	/// <summary>
	/// 获取一个动画节点
	/// </summary>
	/// <returns></returns>
	public static SpineHandler Get() => Scene.Instantiate<SpineHandler>();
	/// <summary>
	/// 等待动画事件
	/// </summary>
	/// <param name="eventName">事件名称</param>
	/// <returns></returns>
	public async Task<float> WaitForAnimEvent(string eventName)
	{
		var tcs = new TaskCompletionSource<float>();

		void Handler(string name, float remaining)
		{
			if (name == eventName)
				tcs.TrySetResult(remaining);
		}

		OnSpineAnimEvent += Handler;
		try { return await tcs.Task; }
		finally { OnSpineAnimEvent -= Handler; }
	}

	/// <summary>
	/// 加载 SpineSkeletonDataResource (.tres)
	/// </summary>
	public void LoadSkeletonData(string tresPath)
	{
		if (SpineSprite == null)
		{
			return;
		}
		Resource data = ResourceLoader.Load(tresPath);
		if (data == null)
		{
			return;
		}
		SpineSprite.Set("skeleton_data_res", data);
		SetTimeScale();
	}

	public void SetTimeScale(float timeScale = 1.25f)
	{
		SpineSprite.Call("rebind_time_scale",timeScale);
	}

	/// <summary>
	/// 设置动画。
	/// </summary>
	public void SetAnimation(int track, string animName, bool loop)
	{
		SpineSprite.Call("set_animation", track, animName, loop);
	}
	/// <summary>
	/// 添加动画。
	/// </summary>
	public void AddAnimation(int track, string animName, bool loop, float delay = 0.0f)
	{
		SpineSprite.Call("add_animation", track, animName, loop, delay);
	}

	public void SetAttachment(string id, string attachment)
	{
		SpineSprite.Call("set_attachment", id, attachment);
	}
	public void SetSkin(string skinName)
	{
		SpineSprite.Call("set_skin", skinName);
	}

    public Vector2 GetBoneWorldScale(string boneName)
    {
        return SpineSprite.Call("get_bone_world_scale", boneName).AsVector2();
    }

    public Vector2 GetBoneScale(string boneName)
    {
        return SpineSprite.Call("get_bone_scale", boneName).AsVector2();
    }

    public Vector2 GetBoneLocalPos(string boneName)
    {
        return SpineSprite.Call("get_bone_local_pos", boneName).AsVector2();
    }

    public Vector2 GetBoneWorldPos(string boneName)
	{
		return SpineSprite.Call("get_bone_world_pos", boneName).AsVector2();
	}

	public event Action<string, float> OnSpineAnimEvent;

	public void ConnectAnimationEvent()
	{
		SpineSprite.Connect("spine_anim_event", Callable.From<string, float>((name, obj) =>
		{
			OnSpineAnimEvent?.Invoke(name, obj);
		}));
	}
	public void ClearTrack(int track)
	{
		if (SpineSprite == null) return;
		SpineSprite.Call("clear_track", track);
	}
	public void ClearTracks()
	{
		if (SpineSprite == null) return;
		SpineSprite.Call("clear_tracks");
	}

	/// <summary>
	/// 设置动画，等待播放完成后销毁自身
	/// </summary>
	public void SetAnimationAndFreeOnEnd(int track, string animName, bool loop = false)
	{
		SetAnimation(track, animName, loop);
		if (loop) return;

		var callable = Callable.From<GodotObject, GodotObject, GodotObject>(
			(sprite, state, entry) => CallDeferred(nameof(QueueFree)));

		SpineSprite.Connect("animation_completed", callable,
			(uint)GodotObject.ConnectFlags.OneShot);
	}

	/// <summary>
	/// 设置动画，等待播放完成。
	/// </summary>
	public async Task SetAnimationTask(int track, string animName, bool loop = false)
	{
		SetAnimation(track, animName, loop);
		if (loop) return;

		var tcs = new TaskCompletionSource<bool>();
		Callable callable = Callable.From<GodotObject, GodotObject, GodotObject>(
			(sprite, state, entry) => tcs.TrySetResult(true));

		SpineSprite.Connect("animation_completed", callable,
			(uint)GodotObject.ConnectFlags.OneShot);

		await tcs.Task;
	}

	/// <summary>
	/// 设置动画，等待播放完成后销毁自身。循环动画会立即返回且不销毁。
	/// </summary>
	public async Task SetAnimationAndFreeOnEndTask(int track, string animName, bool loop = false)
	{
		SetAnimation(track, animName, loop);
		if (loop) return;

		var tcs = new TaskCompletionSource<bool>();
		Callable callable = Callable.From<GodotObject, GodotObject, GodotObject>(
			(sprite, state, entry) => tcs.TrySetResult(true));

		SpineSprite.Connect("animation_completed", callable,
			(uint)GodotObject.ConnectFlags.OneShot);

		await tcs.Task;

		QueueFree();
	}
	/// <summary>
	/// 播放动画，并等待指定事件触发后返回。
	/// loop 为 true 时也会等待事件，不会立即返回。
	/// </summary>
	/// <param name="track">轨道</param>
	/// <param name="animName">动画名</param>
	/// <param name="eventName">要等待的事件名</param>
	/// <param name="loop">是否循环播放</param>
	/// <returns>事件触发时返回 remaining（和 WaitForAnimEvent 一致）</returns>
	public async Task<float> SetAnimationAndWaitEvent(
		int track,
		string animName,
		string eventName,
		bool loop = false)
	{
		var tcs = new TaskCompletionSource<float>();

		void Handler(string name, float remaining)
		{
			if (name == eventName)
				tcs.TrySetResult(remaining);
		}

		OnSpineAnimEvent += Handler;
		try
		{
			SetAnimation(track, animName, loop);
			return await tcs.Task;
		}
		finally
		{
			OnSpineAnimEvent -= Handler;
		}
	}
	/// <summary>
	/// 播放动画，等待指定事件触发后销毁自身。
	/// loop 为 true 时也会等待事件，不会立即返回。
	/// </summary>
	/// <param name="track">轨道</param>
	/// <param name="animName">动画名</param>
	/// <param name="eventName">要等待的事件名</param>
	/// <param name="loop">是否循环播放</param>
	public async Task SetAnimationAndFreeOnEvent(
		int track,
		string animName,
		string eventName,
		bool loop = false)
	{
		var tcs = new TaskCompletionSource<float>();

		void Handler(string name, float remaining)
		{
			if (name == eventName)
				tcs.TrySetResult(remaining);
		}

		OnSpineAnimEvent += Handler;
		try
		{
			SetAnimation(track, animName, loop);
			await tcs.Task;
		}
		finally
		{
			OnSpineAnimEvent -= Handler;
		}

		QueueFree();
	}
	/// <summary>
	/// 获得动画的时长
	/// </summary>
	/// <param name="animName">动画名称</param>
	/// <returns></returns>
	public float GetAnimationDuration(string animName)
	{
		return SpineSprite.Call("get_animation_duration", animName).AsSingle();
	}
	/// <summary>
	/// 递归预加载目录下所有 Spine .tres 资源，填充 Godot 资源缓存，
	/// 避免首次冷加载触发绑定层的 update_mixes 缺陷。
	/// </summary>
	/// <param name="dirPath">资源目录，如 "res://素材(C#)/卡牌"</param>
	public static void Init(string dirPath)
	{
		using var dir = DirAccess.Open(dirPath);
		if (dir == null)
		{
			return;
		}

		dir.ListDirBegin();
		string fileName = dir.GetNext();
		while (fileName != "")
		{
			if (!fileName.StartsWith("."))
			{
				string fullPath = dirPath.PathJoin(fileName);

				if (dir.CurrentIsDir())
				{
					Init(fullPath);
				}
				else if (fileName.EndsWith(".tres"))
				{
					var res = ResourceLoader.Load(fullPath);
				}
			}
			fileName = dir.GetNext();
		}
		dir.ListDirEnd();
	}
	/// <summary>
	/// 停止动画
	/// </summary>
	/// <param name="track"></param>
	public void PauseAnimation(int track)
	{
		SpineSprite.Call("pause_animation", track);
	}
	/// <summary>
	/// 停止动画
	/// </summary>
	/// <param name="track"></param>
	public void PauseAnimation()
	{
		SpineSprite.Call("pause_all_tracks");
	}
	/// <summary>
	/// 判断是否存在指定名称的动画
	/// </summary>
	public bool HasAnimation(string animName)
	{
		if (SpineSprite == null) return false;
		return SpineSprite.Call("has_animation", animName).AsBool();
	}
	/// <summary>
	/// 播放候选列表里第一个存在的动画
	/// </summary>
	/// <param name="track">轨道</param>
	/// <param name="loop">是否循环</param>
	/// <param name="animNames">候选动画名，按优先级排列</param>
	/// <returns>实际播放的动画名，都没找到返回 null</returns>
	public string SetFirstAvailableAnimation(int track, bool loop, params string[] animNames)
	{
		foreach (var name in animNames)
		{
			if (HasAnimation(name))
			{
				SetAnimation(track, name, loop);
				return name;
			}
		}
		return null;
	}
	/// <summary>
	/// 播放候选列表里第一个存在的动画并等待播放完毕
	/// </summary>
	/// <param name="track">轨道</param>
	/// <param name="loop">是否循环</param>
	/// <param name="animNames">候选动画名，按优先级排列</param>
	/// <returns>实际播放的动画名，都没找到返回 null</returns>
	public async Task<string> SetFirstAvailableAnimationTask(int track, bool loop, params string[] animNames)
	{
		foreach (var name in animNames)
		{
			if (HasAnimation(name))
			{
				await SetAnimationTask(track, name, loop);
				return name;
			}
		}
		return null;
	}
	/// <summary>
	/// 播放候选列表里第一个存在的动画并等待事件
	/// </summary>
	/// <param name="track">轨道</param>
	/// <param name="loop">是否循环</param>
	/// <param name="animNames">候选动画名，按优先级排列</param>
	/// <returns>实际播放的动画名，都没找到返回 0</returns>
	public async Task<float> SetFirstAvailableAndWaitEvent(
	int track,
	string eventName,
	bool loop,
	params string[] animNames)
	{
		foreach (var name in animNames)
		{
			if (HasAnimation(name))
			{
				return await SetAnimationAndWaitEvent(track, name, eventName, loop);
			}
		}
		return -1f;
	}
	/// <summary>
	/// 返回候选列表里第一个存在的动画名，都不存在返回 null
	/// </summary>
	public string GetFirstAvailableAnimation(params string[] animNames)
	{
		foreach (var name in animNames)
		{
			if (HasAnimation(name))
				return name;
		}
		return null;
	}

	/// <summary>
	/// 返回候选列表里第一个存在的动画的时长，都不存在返回 0
	/// </summary>
	public float GetFirstAvailableDuration(params string[] animNames)
	{
		var name = GetFirstAvailableAnimation(animNames);
		return name == null ? 0f : GetAnimationDuration(name);
	}
}
