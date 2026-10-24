using Scene;
using System.Threading.Tasks;

/// <summary>
/// 个人信息（二级菜单）。<br/>
/// 注意：作为 SubMenu 的 SubScene，根节点脚本必须继承 <see cref="SubMenuScene"/>，
/// 否则 SceneManager / SubMenu.Instantiate() 会拒绝加载（"根节点不是 SubMenuScene"）。<br/>
/// 节点初始化写在 _Load() 里（_Load 由 SubMenuScene._EnterTree 调用，比 _Ready 早）。
/// </summary>
public partial class UserInfo : SubMenuScene
{
	public override Task _Load()
	{
		// 需要初始化界面就在这里 GetNode / 填数据；
		// 此时子节点的 _Ready 还没跑，不能依赖它们 _Ready 里缓存的字段。
		return Task.CompletedTask;
	}
}
