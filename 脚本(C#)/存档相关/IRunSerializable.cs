using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet;

public interface IRunSerializable
{    
    /// <summary>
    /// 存储成文档的键
    /// </summary>
    string Key { get; }

    /// <summary>
    /// 数据类型
    /// </summary>
    Type DataType { get; }

    /// <summary>
    /// 序列化
    /// </summary>
    /// <returns></returns>
    string ToJsonPair(bool onlyValue = false);

    /// <summary>
    /// 快照
    /// </summary>
    IRunSaveProp Snapshot();
    /// <summary>
    /// 变量化
    /// </summary>
    /// <returns></returns>
    IRunProps ToProps();
}
