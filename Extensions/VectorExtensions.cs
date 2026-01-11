using Microsoft.Xna.Framework;

namespace SKSSL.Extensions;

public static class VectorExtensions
{
    public static bool IsZero(this Vector4 vector) => vector.X == 0 && vector.Y == 0 && vector.Z == 0 && vector.W == 0;
    public static Color AsXnaColor(this Vector4 vector) => new(vector.W, vector.X, vector.Y, vector.Z);
}