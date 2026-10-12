using Frenemypillar.Script.RunSet.RunPropSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;

public class LongSerializable(string key, long data) : RunSerializable<LongProp>
{
    public override string Key => key;

    public override RunProps<LongProp> ToProps()
    {
        return new LongProps(key, data);
    }
    private readonly LongProp _snap = new LongProp(data);
    protected override LongProp SnapshotData => _snap;
}
