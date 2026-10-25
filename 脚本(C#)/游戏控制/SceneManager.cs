using Battle.Entity;
using Card;
using Godot;
using Hero;
using Scene;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Controller;

/// <summary>
/// 控制场景切换的类。<br/>
/// 多级子菜单用「栈」记录打开的每一层（<see cref="Menus"/>），返回时出栈并只释放当前层，
/// 上一层场景本来就在树里，直接露出来即可 —— 不再重新实例化上一层。
/// </summary>
public static class SceneManager
{
	/// <summary>所有已注册的菜单按钮（SubMenu 随场景销毁会自动 Unregister，遍历前再兜底过滤一次失效对象）</summary>
	private static readonly List<SubMenu> AllMenus = new();
	/// <summary>打开的菜单栈：最后一个是当前层</summary>
	private static readonly List<SubMenu> Menus = new();

	/// <summary>跳转/返回过程中为 true，避免连点导致重复入栈出栈</summary>
	private static bool _changing;

	public static void Register(SubMenu menu)
	{
		if (menu == null) return;
		if (!AllMenus.Contains(menu)) AllMenus.Add(menu);
	}
	public static void Unregister(SubMenu menu) => AllMenus.Remove(menu);

	/// <summary>
	/// 当前最上层（正在显示）的场景是不是它。返回按钮只允许退自己这一层：
	/// 否则下层场景的返回按钮一按，会先把下层自己滑走、却把上层弹掉，留下"看得见但点不动"的卡死状态。
	/// </summary>
	public static bool IsCurrent(SubMenuScene scene)
	{
		if (scene == null || Menus.Count == 0) return false;
		return Menus[^1].GetScene() == scene;
	}

	private static TaskCompletionSource<bool> _LoadTask;
	public static Task LoadTask => _LoadTask?.Task ?? Task.CompletedTask;
	public static void InvokeLoadEnd()
	{
		_LoadTask?.TrySetResult(true);
		_LoadTask = null;
	}

	private static void PruneMenus()
	{
		for (int i = AllMenus.Count - 1; i >= 0; i--)
			if (AllMenus[i] == null || !GodotObject.IsInstanceValid(AllMenus[i]))
				AllMenus.RemoveAt(i);
	}

	private static void UnableSubmenu()
	{
		PruneMenus();
		foreach (var subMenu in AllMenus) subMenu.Function(false);
	}

	private static void EnableSubmenu()
	{
		PruneMenus();
		foreach (var subMenu in AllMenus) subMenu.Function(true);
	}

	/// <summary>
	/// 只启用"当前层场景内部"的菜单按钮；被盖住的主界面按钮保持禁用，
	/// 否则场景没盖到的区域还能点到主界面。
	/// </summary>
	private static void EnableMenusIn(SubMenuScene scene)
	{
		PruneMenus();
		bool inTree = scene != null && GodotObject.IsInstanceValid(scene);
		foreach (var subMenu in AllMenus)
			subMenu.Function(inTree && scene.IsAncestorOf(subMenu));
        bool persistNess = inTree && Menus.Count > 0 && Menus[^1].IsPersistNess;
        Control diamond = Main.SceneContainer.GetNodeOrNull<Control>("%钻石");
        diamond.Visible = !persistNess;
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
	/// 打开一个子目录。
	/// </summary>
	/// <param name="subMenu">子目录按钮</param>
	public static async void JumpToSub(SubMenu subMenu)
	{
		if (subMenu == null || _changing) return;
		_changing = true;

		bool showLoading = subMenu.ShowLoading;
		SubMenuScene scene = null;
		bool pushed = false;
		try
		{
			UnableSubmenu();

			if (Menus.Count == 0) await Clear();

			scene = subMenu.Instantiate();
			if (scene == null)
			{
				EnableSubmenu();
				return;
			}
			scene.Visible = false;
			scene.Payload = subMenu.Payload;

			if (showLoading) await Load();

			_LoadTask = new TaskCompletionSource<bool>();
			Main.SceneContainer.GetNode<CanvasLayer>("%UponScene").AddChild(scene);
			if (Menus.Count > 0)
				Menus[^1].Reparent(Main.SceneContainer.GetNode<CanvasLayer>("%SubScene"));
			Menus.Add(subMenu);
			pushed = true;

			await LoadTask;
			scene.Visible = true;
			if (showLoading) await LoadEnd();
		}
		catch (Exception e)
		{
			GD.PushError($"[SceneManager] 打开子菜单失败：{subMenu.SubScene}\n{e}");
			if (pushed && Menus.Count > 0) Menus.RemoveAt(Menus.Count - 1);
			if (scene != null && GodotObject.IsInstanceValid(scene)) scene.QueueFree();
			EnableSubmenu();
		}
		finally
		{
			_changing = false;
		}
	}

	/// <summary>
	/// 直接回到主界面：把菜单栈里的所有层一次性回收（登录成功、退出账号之后用）。<br/>
	/// 和连按返回不同：这里先从上往下播完每一层的 Outro()，再统一清场、播主界面按钮动画，
	/// 中间不会出现"退到一半停在中间层"的状态。
	/// </summary>
	public static async void ReturnToMain()
	{
		if (Menus.Count == 0)
		{
			BackButton.JoinScene(true);
			EnableSubmenu();
			return;
		}
		if (_changing) return;
		_changing = true;

		bool showLoading = Menus[0].ShowLoading;
		var layers = new List<SubMenu>(Menus);
		Menus.Clear();

		try
		{
			for (int i = layers.Count - 1; i >= 0; i--)
			{
				SubMenuScene scene = layers[i].GetScene();
				if (scene == null || !GodotObject.IsInstanceValid(scene)) continue;
				try
				{
					await scene.Outro();
				}
				catch (Exception e)
				{
					GD.PushError($"[SceneManager] 子场景离场动画出错：{e}");
				}
			}

			if (showLoading) await Load();
			await Clear();

			foreach (SubMenu menu in layers)
			{
				if (menu != null && GodotObject.IsInstanceValid(menu)) menu.Destory();
			}

			BackButton.JoinScene(true);
			if (showLoading) await LoadEnd();
			EnableSubmenu();
		}
		catch (Exception e)
		{
			GD.PushError($"[SceneManager] 返回主界面失败：{e}");
			EnableSubmenu();
		}
		finally
		{
			_changing = false;
		}
	}

	/// <summary>
	/// 关闭当前层的子目录
	/// </summary>
	public static async void ReturnToSub(SubMenuScene from = null)
	{
		if (Menus.Count == 0 || _changing) return;
		if (from != null && !IsCurrent(from))
		{
			GD.PushWarning("[SceneManager] 忽略了非当前层的返回请求");
			return;
		}
		_changing = true;

		SubMenu menu = Menus[^1];
		SubMenuScene scene = menu.GetScene();
		Menus.RemoveAt(Menus.Count - 1);

		try
		{
			if (scene != null && GodotObject.IsInstanceValid(scene))
				await scene.Outro();
		}
		catch (Exception e)
		{
			GD.PushError($"[SceneManager] 子场景离场动画出错：{e}");
		}

		try
		{
			if (Menus.Count == 0)
			{
				bool showLoading = menu.ShowLoading;
				if (showLoading) await Load();
				await Clear();
				menu.Destory(); 
				BackButton.JoinScene(true);
				if (showLoading) await LoadEnd();
				EnableSubmenu();
			}
			else
			{
				menu.FreeScene();
				menu.ResetPress();
				SubMenu parentMenu = Menus[^1];
				SubMenuScene parentScene = parentMenu.GetScene();
				if (parentScene != null && GodotObject.IsInstanceValid(parentScene) && parentScene.Visible)
				{
					BackButton.JoinScene(parentScene.HideReturnButton);
					parentMenu.Reparent(Main.SceneContainer.GetNode<CanvasLayer>("%UponScene"));
					EnableMenusIn(parentScene);
				}
				else
				{
					GD.PushWarning("[SceneManager] 上一层场景不可用，已回收所有层并退回主界面");
					foreach (var left in Menus) left.FreeScene();
					Menus.Clear();
					BackButton.JoinScene(true);
					EnableSubmenu();
				}
			}
		}
		catch (Exception e)
		{
			GD.PushError($"[SceneManager] 返回子菜单失败：{e}");
			EnableSubmenu();
		}
		finally
		{
			_changing = false;
		}
	}
}
