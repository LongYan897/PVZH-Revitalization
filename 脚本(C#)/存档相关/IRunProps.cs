using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet;

/// <summary>
/// 本局Run持久属性
/// </summary>
public interface IRunProps
{
    /// <summary>
    /// 属性唯一标识名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Props的类型
    /// </summary>
    Type DataType { get; }

    /// <summary>
    /// 序列化
    /// </summary>
    /// <returns></returns>
    IRunSerializable ToSerializable();
}
