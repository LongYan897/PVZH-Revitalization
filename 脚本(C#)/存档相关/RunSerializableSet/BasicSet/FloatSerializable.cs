using Frenemypillar.Script.RunSet.RunPropSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;

public class FloatSerializable(string key, float data) : RunSerializable<FloatProp>
{
    public override string Key => key;

    public override RunProps<FloatProp> ToProps()
    {
        return new FloatProps(key, data);
    }
    private readonly FloatProp _snap = new FloatProp(data);
    protected override FloatProp SnapshotData => _snap;
}
