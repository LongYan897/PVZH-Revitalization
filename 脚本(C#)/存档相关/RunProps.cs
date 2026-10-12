using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet;

public abstract class RunProps<T> : IRunProps where T : IRunSaveProp
{
    public abstract string Name { get; }
    public Type DataType => typeof(T);
    protected abstract object Data { get; }
    public abstract IRunSerializable ToSerializable();
}
