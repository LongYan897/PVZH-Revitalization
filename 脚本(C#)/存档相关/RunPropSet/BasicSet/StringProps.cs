using Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunPropSet.BasicSet;


public class StringProps(string name, string data) : RunProps<StringProp>
{
    public override string Name => name;

    public override RunSerializable<StringProp> ToSerializable()
    {
        return new StringSerializable(name, data);
    }
    protected override object Data => data;
}

