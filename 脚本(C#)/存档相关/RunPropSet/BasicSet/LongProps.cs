using Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Frenemypillar.Script.RunSet.RunPropSet.BasicSet;

public class LongProps(string name,long data) : RunProps<LongProp>
{
    public override string Name => name;

    public override RunSerializable<LongProp> ToSerializable()
    {
        return new LongSerializable(name, data);
    }
    protected override object Data => data;
}
