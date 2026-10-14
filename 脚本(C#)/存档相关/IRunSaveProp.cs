using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet;

public interface IRunSaveProp
{
    object Value { get;}
}

public struct IntProp(int value) : IRunSaveProp
{
    public int ValueProp = value;

    public object Value => ValueProp;
}

public struct UIntProp(uint value) : IRunSaveProp
{
    public uint ValueProp = value;

    public object Value => ValueProp;
}

public struct FloatProp(float value) : IRunSaveProp
{
    public float ValueProp = value;
    public object Value => ValueProp;
}

public struct StringProp(string value) : IRunSaveProp
{
    public string ValueProp = value;
    public object Value => ValueProp;
}

public struct LongProp(long value) : IRunSaveProp
{
    public long ValueProp = value;

    public object Value => ValueProp;
}

public struct BoolProp(bool value) : IRunSaveProp
{
    public bool ValueProp = value;
    public object Value => ValueProp;
}

public struct ListProp(List<IRunSaveProp> value) : IRunSaveProp
{
    public List<IRunSaveProp> ValueProp = value;
    public object Value => ValueProp;
}

public struct DictionaryProp(List<(string Key, IRunSaveProp)> value) : IRunSaveProp
{
    public List<(string Key, IRunSaveProp SaveProp)> ValueProp = value;
    public object Value => ValueProp;
}