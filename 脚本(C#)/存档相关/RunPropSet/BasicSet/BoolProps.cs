namespace Save;

public class BoolProps(string name, bool data) : RunProps<BoolProp>
{
    public override string Name => name;

    public override RunSerializable<BoolProp> ToSerializable()
    {
        return new BoolSerializable(name, data);
    }

    protected override object Data => data;
}
