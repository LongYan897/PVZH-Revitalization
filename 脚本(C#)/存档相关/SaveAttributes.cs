using System;

namespace Save;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class RunSavedAttribute : Attribute
{
    public string Key { get; }

    public RunSavedAttribute(string key = null)
    {
        Key = key;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class BattleSavedAttribute : Attribute
{
    public string Key { get; }

    public BattleSavedAttribute(string key = null)
    {
        Key = key;
    }
}
