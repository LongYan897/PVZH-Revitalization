using Frenemypillar.Script.RunSet.RunPropSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;

public class StringSerializable(string key, string data) : RunSerializable<StringProp>
{
    public override string Key => key;

    public override RunProps<StringProp> ToProps()
    {
        return new StringProps(key, data);
    }
    private readonly StringProp _snap = new StringProp(data);
    protected override StringProp SnapshotData => _snap;
}