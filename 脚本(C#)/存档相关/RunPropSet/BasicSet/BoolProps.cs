using Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunPropSet.BasicSet;

public class BoolProps(string name,bool data) : RunProps<BoolProp>
{
    public override string Name => name;

    public override RunSerializable<BoolProp> ToSerializable()
    {
        return new BoolSerializable(name, data);
    }

    protected override object Data => data;
}
