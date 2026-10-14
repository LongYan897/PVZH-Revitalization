using Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunPropSet.BasicSet;

public class ListProps(string name, List<IRunSaveProp> data) : RunProps<ListProp>
{
    public override string Name => name;

    public override RunSerializable<ListProp> ToSerializable()
    {
        return new ListSerializable(name, data);
    }

    protected override object Data => data;
}
