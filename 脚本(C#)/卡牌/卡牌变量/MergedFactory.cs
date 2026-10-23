using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;


public static class MergedFactory
{
    /// <summary>
    /// 合并数个攻击力类型
    /// </summary>
    /// <param name="atkType">被合并的对象</param>
    /// <param name="types">要合并的对象</param>
    /// <returns></returns>
    public static MergedAtkType Merge(this AtkType atkType, params AtkType[] types) => new([atkType, .. types]);
    /// <summary>
    /// 合并数个攻击力类型
    /// </summary>
    /// <param name="atkType">被合并的对象</param>
    /// <param name="types">要合并的对象</param>
    /// <returns></returns>
    public static MergedHpType Merge(this HpType atkType, params HpType[] types) => new([atkType, .. types]);
}