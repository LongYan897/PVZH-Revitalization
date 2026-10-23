using Godot;
using Scene;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 卡包详情：从卡包列表的「详情」按钮打开，根节点必须是 SubMenuScene 的子类，
/// 否则 SubMenu.Instantiate() 里的 Instantiate&lt;SubMenuScene&gt;() 会抛 InvalidCastException。<br/>
/// 进入场景树时 SubMenuScene._EnterTree() 会先调 BackButton.JoinScene(HideReturnButton)，
/// 再 await _Load()，所以节点初始化写在这里，不要放 _Ready。<br/>
/// 这里先只保证能打开；数据填充、滑入滑出动画按需往 _Load() 里加。
/// </summary>
public partial class CardPackDetail : SubMenuScene
{
	public override Task _Load()
	{
		return Task.CompletedTask;
	}
}
