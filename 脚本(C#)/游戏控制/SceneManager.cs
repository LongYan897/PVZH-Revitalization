using Battle.Entity;
using Card;
using Godot;
using Hero;
using Scene;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Controller;
/// <summary>
/// 控制场景切换的类
/// </summary>
public static class SceneManager
{
	/// <summary>
	/// 当前打开的子目录
	/// </summary>
	private static readonly Dictionary<SubMenu, SubMenu> Menus = new();
	private static readonly List<SubMenu> AllMenus = new();
	public static void Register(SubMenu menu) => AllMenus.Add(menu);
	private static TaskCompletionSource<bool> _LoadTask;
	public static Task LoadTask => _LoadTask?.Task ?? Task.CompletedTask;
	public static void InvokeLoadEnd()
	{
		_LoadTask?.TrySetResult(true);
		_LoadTask = null;
	}
	private static async Task Load()
	{
		Main.Load.Modulate = new(1, 1, 1, 0);
		Tween tween = Main.Load.CreateTween();
		tween.TweenProperty(Main.Load, "modulate", new Color(1, 1, 1, 1), 0.2f);
		await Task.WhenAll(
			Main.Load.ToSignal(tween, Tween.SignalName.Finished).ToTask(),
			Main.Load.LoadIn());
	}
	private static void UnableSubmenu()
	{
		foreach (var subMenu in AllMenus)
		{
			subMenu.Function(false);
		}
	}
	private static void EnableSubmenu()
	{
		foreach (var subMenu in AllMenus)
		{
			subMenu.Function(true);
		}
	}
	public static Task ToTask(this SignalAwaiter a)
	{
		var tcs = new TaskCompletionSource();
		_ = Await();
		return tcs.Task;

		async Task Await()
		{
			await a;
			tcs.SetResult();
		}
	}
	private static async Task LoadEnd()
	{
		Tween tween = Main.Load.CreateTween();
		tween.TweenProperty(Main.Load, "modulate", new Color(1, 1, 1, 0), 0.2f);
		await Main.Load.ToSignal(tween, Tween.SignalName.Finished);
		Main.Load.LoadOut();
	}
	private static Task Clear()
	{
		NodeCard.Clear();
		HeroInstance.Clear();
		Fighter.ClearAll();
		CardDes.Close();
		return Task.CompletedTask;
	}
	/// <summary>
	/// 打开一个子目录
	/// </summary>
	/// <param name="subMenu">子目录</param>
	public static async void JumpToSub(SubMenu subMenu)
	{
		if (subMenu == null) return;
		UnableSubmenu();
		await Clear();
		var parent =
			Main.SceneContainer;
        var subParent =
            Main.SubSceneContainer;
        _LoadTask = new TaskCompletionSource<bool>();
		var scene = subMenu.Instantiate();
		scene.Visible = false;
		// 只有勾了 ShowLoading 的“整屏场景”才过加载界面，二级面板直接进
		bool showLoading = subMenu.ShowLoading;
		if (showLoading)
			await Load();
		if (Menus.Count == 0)
		{
			Menus.Add(subMenu, null);
			parent.GetNode<CanvasLayer>("%UponScene").AddChild(scene);
		}
		else
		{
			var last = Menus.Last().Key;
			Menus[subMenu] = last;
			last.Reparent(subParent);
			parent.GetNode<CanvasLayer>("%UponScene").AddChild(scene);
		}
		await LoadTask;
		scene.Visible = true;
		if (showLoading)
			await LoadEnd();
	}
	/// <summary>
	/// 关闭顶层子目录
	/// </summary>
	public static async void ReturnToSub()
	{
		if (Menus.Count == 0) return;
		var last = Menus.Last();
		Menus.Remove(last.Key);
		if (last.Value == null)
		{
			// 退出的菜单是整屏场景时，回主界面才过加载界面
			bool showLoading = last.Key.ShowLoading;
			if (showLoading)
				await Load();
			await Clear();
			var parent = Main.SceneContainer;
			last.Key.Destory();
			if (showLoading)
				await LoadEnd();
			EnableSubmenu();
		}
		else
		{
			JumpToSub(last.Value);
		}
	}
}
