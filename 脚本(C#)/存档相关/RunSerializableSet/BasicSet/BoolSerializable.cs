using Frenemypillar.Script.RunSet.RunPropSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;

public class BoolSerializable(string key, bool data) : RunSerializable<BoolProp>
{
    public override string Key => key;

    public override RunProps<BoolProp> ToProps()
    {
        return new BoolProps(key, data);
    }
    private readonly BoolProp _snap = new BoolProp(data);
    protected override BoolProp SnapshotData => _snap;
}