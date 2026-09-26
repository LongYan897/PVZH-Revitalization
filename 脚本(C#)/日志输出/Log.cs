using Godot;

namespace Logger;

/// <summary>
/// 发送日志的类
/// </summary>
public static class Log
{
    public static int DebugId = 0;
    /// <summary>
    /// 发送错误日志
    /// </summary>
    /// <param name="content">发送的内容</param>
    public static void Error(string content)
    {
        GD.PrintErr(content);
    }
    /// <summary>
    /// 发送用于修复bug的日志
    /// </summary>
    /// <param name="content">发送的内容</param>
    /// <param name="debugId">修复bug的代号,需要和DebugId相同才会发送日志</param>
    public static void Debug(string content,int debugId)
    {
        if (debugId == DebugId)
            GD.Print(content);
    }
}
