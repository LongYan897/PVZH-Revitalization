using Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet.RunPropSet.BasicSet;

public class DictionaryProps(string name, List<(string key,IRunSaveProp saveProp)> data) : RunProps<DictionaryProp>
{
    public override string Name => name;

    public override RunSerializable<DictionaryProp> ToSerializable()
    {
        return new DictionarySerializable(name, data);
    }

    protected override object Data => data;
}
