namespace Save;

public class BoolSerializable(string key, bool data) : RunSerializable<BoolProp>
{
    public override string Key => key;

    public override RunProps<BoolProp> ToProps()
    {
        return new BoolProps(key, data);
    }
    private readonly BoolProp _snap = new BoolProp(data);
    protected override BoolProp SnapshotData => _snap;
}