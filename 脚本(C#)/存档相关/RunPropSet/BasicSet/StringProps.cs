namespace Save;

public class StringProps(string name, string data) : RunProps<StringProp>
{
    public override string Name => name;

    public override RunSerializable<StringProp> ToSerializable()
    {
        return new StringSerializable(name, data);
    }
    protected override object Data => data;
}

