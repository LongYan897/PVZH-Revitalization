using Controller;
using Godot;
using System;
using System.Threading.Tasks;

namespace Scene;

/// <summary>
/// 子场景（多级菜单里的“一层”）。<br/>
/// 根节点必须是它的子类，SceneManager 才能把这一层压进菜单栈里。
/// </summary>
public abstract partial class SubMenuScene : Node2D
{
	public sealed override async void _EnterTree()
	{
		if (IsOpening) return;
		IsOpening = true;
		BackButton.JoinScene(HideReturnButton);
		try
		{
			await _Load();
		}
		catch (Exception e)
		{
			GD.PushError($"[SubMenuScene] {Name} 的 _Load() 出错：{e}");
		}
		finally
		{
			// 无论成功失败都要放行，否则 SceneManager 会永远卡在 await LoadTask，
			// 表现就是场景不显示、所有按钮变灰点不动。
			SceneManager.InvokeLoadEnd();
		}
	}

	public abstract Task _Load();

	/// <summary>
	/// 打开这一层时由 <see cref="SubMenu.Payload"/> 传进来的数据。<br/>
	/// SceneManager 会在场景加入树（也就是 _Load 执行）之前把它塞进来，所以 _Load 里能直接读到。
	/// </summary>
	public object Payload { get; set; }

	/// <summary>
	/// 离开这一层之前播放的动画（滑出、淡出……）。<br/>
	/// SceneManager 会 await 它，播完才释放本层场景，所以返回动画写在这里。
	/// </summary>
	public virtual Task Outro() => Task.CompletedTask;

	/// <summary>是否隐藏主界面那个全局返回按钮（本层自带返回按钮时返回 true）</summary>
	public virtual bool HideReturnButton { get; } = false;

	public bool IsOpening;
}
