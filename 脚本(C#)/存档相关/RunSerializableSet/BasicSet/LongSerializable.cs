namespace Save;

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
