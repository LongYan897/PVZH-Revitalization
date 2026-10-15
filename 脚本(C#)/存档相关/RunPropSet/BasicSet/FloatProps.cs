namespace Save;

public class FloatProps(string name, float data) : RunProps<FloatProp>
{
    public override string Name => name;

    public override RunSerializable<FloatProp> ToSerializable()
    {
        return new FloatSerializable(name, data);
    }
    protected override object Data => data;
}
