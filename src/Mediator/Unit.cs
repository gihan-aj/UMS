using System;

namespace Mediator
{
    /// <summary>
    /// Represents a void type.
    /// </summary>
    public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
    {
        public static readonly Unit Value = new();

        public int CompareTo(Unit other) => 0;

        int IComparable.CompareTo(object? obj) => 0;

        public bool Equals(Unit other) => true;

        public override bool Equals(object? obj) => obj is Unit;

        public override int GetHashCode() => 0;

        public override string ToString() => "()";

        public static bool operator ==(Unit left, Unit right) => left.Equals(right);

        public static bool operator !=(Unit left, Unit right) => !left.Equals(right);
    }
}
