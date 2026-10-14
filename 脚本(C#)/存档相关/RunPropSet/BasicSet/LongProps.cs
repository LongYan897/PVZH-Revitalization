namespace Save;

public class LongProps(string name, long data) : RunProps<LongProp>
{
    public override string Name => name;

    public override RunSerializable<LongProp> ToSerializable()
    {
        return new LongSerializable(name, data);
    }
    protected override object Data => data;
}
