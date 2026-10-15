using System.Collections.Generic;

namespace Save;

public class ListProps(string name, List<IRunSaveProp> data) : RunProps<ListProp>
{
    public override string Name => name;

    public override RunSerializable<ListProp> ToSerializable()
    {
        return new ListSerializable(name, data);
    }

    protected override object Data => data;
}
