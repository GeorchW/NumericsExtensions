using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SimpleNumerics.Generator
{
    static class Program
    {
        public static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            var generators = new List<VectorGenerator>();
            string[][] swizzles = new[] {
                new[]{ "YX" },
                new[]{ "XY", "XZ", "YZ" },
                new[]{ "XYZ" }
            };
            for (int i = 2; i <= 4; i++)
            {
                generators.Add(new VectorGenerator("Vector", "float", i,
                    new[] { "-" },
                    new[] { "+", "-", "*", "/", "%" },
                    new[] { ("Zero", "0"), ("One", "1") },
                    "1", "0",
                    new[] { ("Int" + i, "int", false) },
                    swizzles[i - 2],
                    i > 2));
                generators.Add(new VectorGenerator("Int", "int", i,
                    new[] { "-", "~" },
                    new[] { "+", "-", "*", "/", "%" },
                    new[] { ("Zero", "0"), ("One", "1") },
                    "1", "0",
                    new[] { ("Vector" + i, "float", true) },
                    swizzles[i - 2],
                    i > 2));
                generators.Add(new VectorGenerator("Bool", "bool", i,
                    new[] { "!" },
                    new[] { "&", "|", "^" },
                    new[] { ("False", "false"), ("True", "true") },
                    "true", "false",
                    new (string, string, bool)[0],
                    swizzles[i - 2],
                    false));
            }
            foreach (var generator in generators)
            {
                generator.GenerateAll();
            }
        }
    }
}
