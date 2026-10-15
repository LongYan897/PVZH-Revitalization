using System.Collections.Generic;

namespace Save;

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