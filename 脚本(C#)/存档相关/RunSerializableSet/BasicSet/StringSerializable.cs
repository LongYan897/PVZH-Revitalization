namespace Save;

public class StringSerializable(string key, string data) : RunSerializable<StringProp>
{
    public override string Key => key;

    public override RunProps<StringProp> ToProps()
    {
        return new StringProps(key, data);
    }
    private readonly StringProp _snap = new StringProp(data);
    protected override StringProp SnapshotData => _snap;
}