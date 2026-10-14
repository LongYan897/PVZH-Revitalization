

namespace Save;

public class IntProps(string name, int data) : RunProps<IntProp>
{
    public override string Name => name;

    public override RunSerializable<IntProp> ToSerializable()
    {
        return new IntSerializable(name, data);
    }
    protected override object Data => data;
}
