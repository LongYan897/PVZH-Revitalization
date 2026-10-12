using Frenemypillar.Script.RunSet.RunPropSet.BasicSet;

namespace Frenemypillar.Script.RunSet.RunSerializableSet.BasicSet;

public class IntSerializable(string key,int data) : RunSerializable<IntProp>
{
    public override string Key => key;

    public override RunProps<IntProp> ToProps()
    {
        return new IntProps(key, data);
    }
    private readonly IntProp _snap = new IntProp(data);
    protected override IntProp SnapshotData => _snap;
}
