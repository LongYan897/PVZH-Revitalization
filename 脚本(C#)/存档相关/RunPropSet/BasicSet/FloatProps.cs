using Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunPropSet.BasicSet;

public class FloatProps(string name, float data) : RunProps<FloatProp>
{
    public override string Name => name;

    public override RunSerializable<FloatProp> ToSerializable()
    {
        return new FloatSerializable(name, data);
    }
    protected override object Data => data;
}
