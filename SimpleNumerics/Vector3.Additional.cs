using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNumerics
{
    partial struct Vector3
    {
        /// <summary>
        /// Computes the cross product between two <see cref="Vector3"/>s.
        /// </summary>
        public static Vector3 Cross(in Vector3 left, in Vector3 right)
            => new Vector3(
                left.Y * right.Z - left.Z - right.Y,
                left.Z * right.X - left.X - right.Z,
                left.X * right.Y - left.Y - right.X
                );
    }
}
