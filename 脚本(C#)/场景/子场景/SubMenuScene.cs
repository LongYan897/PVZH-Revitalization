using Controller;
using Godot;
using System.Threading.Tasks;

namespace Scene;

/// <summary>
/// 子场景
/// </summary>
public abstract partial class SubMenuScene : Node2D
{
	public sealed override async void _EnterTree()
	{
		if (IsOpening) return;
		IsOpening = true;
		BackButton.JoinScene(HideReturnButton);
		await _Load();
		SceneManager.InvokeLoadEnd();
	}
	public abstract Task _Load();
	/// <summary>
	/// 是否隐藏返回按钮
	/// </summary>
	public virtual bool HideReturnButton { get; } = false;
	public bool IsOpening;
}
