using System;

namespace Save;

public abstract class RunProps<T> : IRunProps where T : IRunSaveProp
{
    public abstract string Name { get; }
    public Type DataType => typeof(T);
    protected abstract object Data { get; }
    public abstract IRunSerializable ToSerializable();
}
