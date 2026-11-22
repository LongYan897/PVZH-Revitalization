using System;
using System.Globalization;

namespace Pack;

public struct DotInt(long value) : IEquatable<DotInt>, IComparable<DotInt>
{
    public long Value = value;

    public static implicit operator DotInt(long v) => new(v);
    public static implicit operator DotInt(int v) => new(v);
    public static implicit operator DotInt(byte v) => new(v);
    public static implicit operator DotInt(sbyte v) => new(v);
    public static implicit operator DotInt(short v) => new(v);
    public static implicit operator DotInt(ushort v) => new(v);
    public static implicit operator DotInt(uint v) => new(v);
    public static implicit operator DotInt(char v) => new(v);

    public static explicit operator DotInt(ulong v) => new((long)v);
    public static explicit operator DotInt(float v) => new((long)v);
    public static explicit operator DotInt(double v) => new((long)v);
    public static explicit operator DotInt(decimal v) => new((long)v);
    public static explicit operator DotInt(string v) =>
    string.IsNullOrWhiteSpace(v)
        ? new(0)
        : new(long.Parse(v, NumberStyles.AllowThousands | NumberStyles.Integer,
                         CultureInfo.InvariantCulture));
    public static implicit operator long(DotInt d) => d.Value;
    public static implicit operator int(DotInt d) => (int)d.Value;
    public static implicit operator string(DotInt d) => d.ToString();

    public readonly T To<T>()
    {
        var t = typeof(T);
        if (t == typeof(DotInt)) return (T)(object)this;
        if (t == typeof(long)) return (T)(object)Value;
        if (t == typeof(int)) return (T)(object)(int)Value;
        if (t == typeof(byte)) return (T)(object)(byte)Value;
        if (t == typeof(sbyte)) return (T)(object)(sbyte)Value;
        if (t == typeof(short)) return (T)(object)(short)Value;
        if (t == typeof(ushort)) return (T)(object)(ushort)Value;
        if (t == typeof(uint)) return (T)(object)(uint)Value;
        if (t == typeof(ulong)) return (T)(object)(ulong)Value;
        if (t == typeof(char)) return (T)(object)(char)Value;
        if (t == typeof(float)) return (T)(object)(float)Value;
        if (t == typeof(double)) return (T)(object)(double)Value;
        if (t == typeof(decimal)) return (T)(object)(decimal)Value;
        if (t == typeof(string)) return (T)(object)ToString();
        if (t == typeof(object)) return (T)(object)Value;
        throw new InvalidCastException($"无法将 DotInt 转换为 {t.Name}");
    }

    public override readonly string ToString()
    {
        return Value.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
    }

    public bool Equals(DotInt other) => Value == other.Value;
    public override bool Equals(object obj) => obj is DotInt other && Equals(other);
    public override readonly int GetHashCode() => Value.GetHashCode();
    public readonly int CompareTo(DotInt other) => Value.CompareTo(other.Value);

    public static bool operator ==(DotInt left, DotInt right) => left.Value == right.Value;
    public static bool operator !=(DotInt left, DotInt right) => left.Value != right.Value;
    public static bool operator <(DotInt left, DotInt right) => left.Value < right.Value;
    public static bool operator >(DotInt left, DotInt right) => left.Value > right.Value;
    public static bool operator <=(DotInt left, DotInt right) => left.Value <= right.Value;
    public static bool operator >=(DotInt left, DotInt right) => left.Value >= right.Value;

    public static DotInt operator +(DotInt a, DotInt b) => new(a.Value + b.Value);
    public static DotInt operator -(DotInt a, DotInt b) => new(a.Value - b.Value);
    public static DotInt operator *(DotInt a, DotInt b) => new(a.Value * b.Value);
    public static DotInt operator /(DotInt a, DotInt b) => new(a.Value / b.Value);
    public static DotInt operator %(DotInt a, DotInt b) => new(a.Value % b.Value);
}