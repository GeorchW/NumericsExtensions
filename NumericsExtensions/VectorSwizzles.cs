using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace NumericsExtensions
{
    /// <summary>
    /// Provides a subset of all possible swizzles as extension methods to the BCL Vector types.
    /// </summary>
    public static class VectorSwizzles
    {
        /// <summary>
        /// Swizzles this <see cref="Vector2"/>, swapping the X and Y components.
        /// </summary>
        public static Vector2 YX(this Vector2 vector) => new Vector2(vector.Y, vector.X);

        /// <summary>
        /// Swizzles this <see cref="Vector3"/>, returning a vector with only the X and Y components.
        /// </summary>
        public static Vector2 XY(this Vector3 vector) => new Vector2(vector.X, vector.Y);
        /// <summary>
        /// Swizzles this <see cref="Vector3"/>, returning a vector with only the X and Z components.
        /// </summary>
        public static Vector2 XZ(this Vector3 vector) => new Vector2(vector.X, vector.Z);
        /// <summary>
        /// Swizzles this <see cref="Vector3"/>, returning a vector with only the Y and Z components.
        /// </summary>
        public static Vector2 YZ(this Vector3 vector) => new Vector2(vector.Y, vector.Z);


        /// <summary>
        /// Swizzles this <see cref="Vector4"/>, returning a <see cref="Vector3"/> with only the first three components.
        /// </summary>
        public static Vector3 XYZ(this Vector4 vector) => new Vector3(vector.X, vector.Y, vector.Z);
    }
}
