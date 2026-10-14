using Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunPropSet.BasicSet;

public class UIntProps(string name,uint data) : RunProps<UIntProp>
{
    public override string Name => name;

    public override RunSerializable<UIntProp> ToSerializable()
    {
        return new UIntSerializable(name,data);
    }

    protected override object Data => data;
}
