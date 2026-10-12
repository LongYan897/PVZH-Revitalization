using Frenemypillar.Script.RunSet.RunPropSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;

public class ListSerializable(string key, List<IRunSaveProp> data) : RunSerializable<ListProp>
{
    public override string Key => key;

    public override RunProps<ListProp> ToProps()
    {
        return new ListProps(key, data);
    }
    private readonly ListProp _snap = new ListProp(data);
    protected override ListProp SnapshotData => _snap;
}