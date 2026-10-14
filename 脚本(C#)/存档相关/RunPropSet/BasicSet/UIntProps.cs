namespace Save;

public class UIntProps(string name, uint data) : RunProps<UIntProp>
{
    public override string Name => name;

    public override RunSerializable<UIntProp> ToSerializable()
    {
        return new UIntSerializable(name, data);
    }

    protected override object Data => data;
}
