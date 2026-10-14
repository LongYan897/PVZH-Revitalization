using Frenemypillar.Script.RunSet.RunPropSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;

public class UIntSerializable(string key,uint data) : RunSerializable<UIntProp>
{
    public override string Key => key;

    public override RunProps<UIntProp> ToProps()
    {
        return new UIntProps(key, data);
    }

    protected override UIntProp SnapshotData => new UIntProp(data);
}
