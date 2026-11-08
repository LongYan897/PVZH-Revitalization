using System;
using System.Globalization;

namespace Pack;

public struct DotInt(long value) : IEquatable<DotInt>, IComparable<DotInt>
{
    public long Value = value;

    public static implicit operator DotInt(long v) => new(v);
    public static implicit operator DotInt(int v) => new(v);
    public static implicit operator long(DotInt d) => d.Value;
    public static implicit operator int(DotInt d) => (int)d.Value;
    public static implicit operator string(DotInt d) => d.ToString();

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

    public static DotInt operator +(DotInt a, DotInt b) => new DotInt(a.Value + b.Value);
    public static DotInt operator -(DotInt a, DotInt b) => new DotInt(a.Value - b.Value);
    public static DotInt operator *(DotInt a, DotInt b) => new DotInt(a.Value * b.Value);
    public static DotInt operator /(DotInt a, DotInt b) => new DotInt(a.Value / b.Value);
    public static DotInt operator %(DotInt a, DotInt b) => new DotInt(a.Value % b.Value);
}
