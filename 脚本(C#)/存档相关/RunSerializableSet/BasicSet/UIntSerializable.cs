namespace Save;

public class UIntSerializable(string key, uint data) : RunSerializable<UIntProp>
{
    public override string Key => key;

    public override RunProps<UIntProp> ToProps()
    {
        return new UIntProps(key, data);
    }

    protected override UIntProp SnapshotData => new UIntProp(data);
}
