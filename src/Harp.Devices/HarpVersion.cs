using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Harp.Devices;

[JsonConverter(typeof(HarpVersionJsonConverter))]
public struct HarpVersion : IEquatable<HarpVersion>, IComparable<HarpVersion>, IComparisonOperators<HarpVersion, HarpVersion, bool>
{
    public readonly byte Major;
    public readonly byte Minor;
    public readonly byte Patch;

    public HarpVersion(byte major, byte minor, byte patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public static bool TryParse(ReadOnlySpan<char> s, out HarpVersion result)
    {
        // Major
        int index = s.IndexOf('.');
        if (index < 0 || !byte.TryParse(s.Slice(0, index), out byte major))
        {
            result = default;
            return false;
        }

        // Minor
        s = s.Slice(index + 1);
        index = s.IndexOf('.');
        if (index < 0 || !byte.TryParse(s.Slice(0, index), out byte minor))
        {
            result = default;
            return false;
        }

        // Patch
        s = s.Slice(index + 1);
        if (!byte.TryParse(s.Slice(0, index), out byte patch))
        {
            result = default;
            return false;
        }

        result = new HarpVersion(major, minor, patch);
        return true;
    }

    public bool Equals(HarpVersion other)
        => this.Major == other.Major
        && this.Minor == other.Minor
        && this.Patch == other.Patch;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is HarpVersion other ? Equals(other) : false;

    public int CompareTo(HarpVersion other)
    {
        if (this.Major > other.Major)
            return 1;
        else if (this.Major < other.Major)
            return -1;
        else if (this.Minor > other.Minor)
            return 1;
        else if (this.Minor < other.Minor)
            return -1;
        else if (this.Patch > other.Patch)
            return 1;
        else if (this.Patch < other.Patch)
            return -1;
        else
            return 0;
    }

    public override string ToString()
        => $"{Major}.{Minor}.{Patch}";

    public override int GetHashCode()
        => Major << 16 | Minor << 8 | Patch;

    public static bool operator >(HarpVersion left, HarpVersion right)
        => left.CompareTo(right) > 0;
    public static bool operator >=(HarpVersion left, HarpVersion right)
        => left.CompareTo(right) >= 0;
    public static bool operator <(HarpVersion left, HarpVersion right)
        => left.CompareTo(right) < 0;
    public static bool operator <=(HarpVersion left, HarpVersion right)
        => left.CompareTo(right) <= 0;
    public static bool operator ==(HarpVersion left, HarpVersion right)
        => left.Equals(right);
    public static bool operator !=(HarpVersion left, HarpVersion right)
        => !(left == right);

    private sealed class HarpVersionJsonConverter : JsonConverter<HarpVersion>
    {
        public override HarpVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? s = reader.GetString();
            if (s is not null && TryParse(s, out HarpVersion result))
                return result;
            else
                throw new FormatException($"'{s}' could not be parsed as a {nameof(HarpVersion)}.");
        }

        public override void Write(Utf8JsonWriter writer, HarpVersion value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}
