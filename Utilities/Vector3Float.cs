namespace SKSSL.Utilities;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
/// <summary>
/// A vector of three float values: (<see cref="X"/>, <see cref="Y"/> <see cref="Z"/>)
/// </summary>
public readonly struct Vector3Float : IEquatable<Vector3Float>
{
    private readonly int X;
    private readonly int Y;
    private readonly int Z;

    /// <summary>
    /// Instantiates an instance of <see cref="Vector3Float"/> with all values 0.
    /// </summary>
    public Vector3Float()
    {
        X = 0;
        Y = 0;
        Z = 0;
    }

    /// <summary>
    /// Instantiates an instance of <see cref="Vector3Float"/> with all values equal to the provided value.
    /// </summary>
    public Vector3Float(int val)
    {
        X = val;
        Y = val;
        Z = val;
    }

    /// <summary>
    /// Instantiates an instance of <see cref="Vector3Float"/> with x and y equal to the provided values, with z set 0.
    /// </summary>
    public Vector3Float(int x, int y)
    {
        X = x;
        Y = y;
        Z = 0;
    }

    /// <summary>
    /// Instantiates an instance of <see cref="Vector3Float"/> with provided x, y, and z values.
    /// </summary>
    public Vector3Float(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    #region Operator Overloads

    public static Vector3Float operator +(Vector3Float a, Vector3Float b) =>
        new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3Float operator -(Vector3Float a, Vector3Float b) =>
        new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static bool operator ==(Vector3Float a, Vector3Float b) =>
        a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    public static bool operator !=(Vector3Float a, Vector3Float b) => !(a == b);

    #endregion

    #region Handy Methods

    public override bool Equals(object? obj) => obj is Vector3Float v && this == v;
    public bool Equals(Vector3Float other) => X == other.X && Y == other.Y && Z == other.Z;

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X}, {Y}, {Z})";

    public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public int ManhattanDistance(Vector3Float other) =>
        Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Z - other.Z);

    /// <returns>
    /// If this <see cref="Vector3Float"/>'s values all equal 0.
    /// </returns>
    public bool IsZero => X == 0 && Y == 0 && Z == 0;

    #endregion
}