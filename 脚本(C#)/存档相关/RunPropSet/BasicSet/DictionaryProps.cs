using System.Collections.Generic;

namespace Save;

public class DictionaryProps(string name, List<(string key, IRunSaveProp saveProp)> data) : RunProps<DictionaryProp>
{
    public override string Name => name;

    public override RunSerializable<DictionaryProp> ToSerializable()
    {
        return new DictionarySerializable(name, data);
    }

    protected override object Data => data;
}
