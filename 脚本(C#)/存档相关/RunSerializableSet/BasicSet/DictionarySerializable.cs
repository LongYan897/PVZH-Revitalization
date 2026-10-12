using Frenemypillar.Script.RunSet.RunPropSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;

public class DictionarySerializable(string key, List<(string key, IRunSaveProp saveProp)> data) : RunSerializable<DictionaryProp>
{
    public override string Key => key;

    public override RunProps<DictionaryProp> ToProps()
    {
        return new DictionaryProps(key, data);
    }
    private readonly DictionaryProp _snap = new DictionaryProp(data);
    protected override DictionaryProp SnapshotData => _snap;
}