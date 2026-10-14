namespace Save;

public class FloatSerializable(string key, float data) : RunSerializable<FloatProp>
{
    public override string Key => key;

    public override RunProps<FloatProp> ToProps()
    {
        return new FloatProps(key, data);
    }
    private readonly FloatProp _snap = new FloatProp(data);
    protected override FloatProp SnapshotData => _snap;
}
