namespace Card;

/// <summary>
/// 打开开包界面时，从购买按钮（<see cref="SubMenu.Payload"/>）传给开包界面的参数。
/// </summary>
/// <param name="PackName">cardPack.json 里的卡包对象名（例如 "Pack1"）</param>
/// <param name="PullCount">这个价格档位抽几张（Options 里的 PullCount）</param>
/// <param name="CardTexturePath">卡包封面图路径（3D 卡包用的贴图）</param>
public sealed record OpenPackPayload(string PackName, int PullCount, string CardTexturePath = null);
