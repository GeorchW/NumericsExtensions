using System;
using System.Collections.Generic;
using System.Linq;

namespace NumericsExtensions.Generator
{
    public class VectorGenerator
    {
        static string[] componentNames = new string[] { "X", "Y", "Z", "W" };
        string baseName;
        string componentType;
        int numComponents;
        string typeName;
        CodeBuilder codeBuilder = new CodeBuilder();

        string[] supportedUnaryOperators;
        string[] supportedBinaryOperators;
        private readonly (string componentName, string vectorName)[] binaryComponentUpcasts;
        (string propertyName, string componentConstant)[] unitProperties;
        string positiveConstant;
        string negativeConstant;
        (string vectorType, string componentType, bool isImplicit)[] casts;
        string[] foreignCasts;
        string[] swizzles;
        bool useIn;

        IEnumerable<string> components => componentNames.Take(numComponents);

        static string See(string s) => $"<see cref=\"{s}\" />";

        string New(Func<string, string> componentSelector, string vectorName = null)
        {
            vectorName = vectorName ?? this.typeName;
            return $"new {vectorName}({string.Join(", ", componentNames.Take(numComponents).Select(componentSelector))})";
        }
        void WriteSummary(string summary)
        {
            codeBuilder.WriteLine("/// <summary>");
            codeBuilder.WriteLine("/// " + summary);
            codeBuilder.WriteLine("/// </summary>");
        }
        string VectorParam(string name, string typeName = null)
        {
            typeName ??= this.typeName;
            return useIn ? $"in {typeName} {name}" : $"{typeName} {name}";
        }

        public VectorGenerator(string baseName, string componentType, int numComponents,
            string[] supportedUnaryOperators, string[] supportedBinaryOperators,
            (string componentName, string vectorName)[] binaryComponentUpcasts,
            (string propertyName, string componentConstant)[] unitProperties,
            string positiveConstant, string negativeConstant,
            (string vectorType, string componentType, bool isImplicit)[] casts,
            string[] foreignCasts,
            string[] swizzles,
            bool useIn
            )
        {
            this.baseName = baseName;
            this.componentType = componentType;
            this.numComponents = numComponents;
            this.supportedUnaryOperators = supportedUnaryOperators;
            this.supportedBinaryOperators = supportedBinaryOperators;
            this.binaryComponentUpcasts = binaryComponentUpcasts;
            this.unitProperties = unitProperties;
            this.positiveConstant = positiveConstant;
            this.negativeConstant = negativeConstant;
            this.casts = casts;
            this.foreignCasts = foreignCasts;
            this.swizzles = swizzles;
            this.useIn = useIn;
            typeName = baseName + numComponents;
        }

        void Generate(string postfix, params Action[] generators)
        {
            codeBuilder = new CodeBuilder();
            codeBuilder.WriteLine("using System;");
            codeBuilder.WriteLine("using System.Diagnostics;");
            codeBuilder.WriteLine("using System.ComponentModel;");
            codeBuilder.WriteLine("using System.Numerics;");
            codeBuilder.WriteLine("");

            using (codeBuilder.Block("namespace NumericsExtensions"))
            {
                if (postfix == null)
                {
                    WriteSummary($"Represents a vector with {numComponents} components of type {See(componentType)}.");
                }
                using (codeBuilder.Block($"public partial struct {typeName}"))
                {
                    foreach (var generator in generators)
                    {
                        generator();
                    }
                }
            }
            string filename = postfix == null ?
                $"./Generated/{typeName}.cs" :
                $"./Generated/{typeName}.{postfix}.cs";
            System.IO.Directory.CreateDirectory("./Generated/");
            System.IO.File.WriteAllText(filename, codeBuilder.ToString());
        }

        public void GenerateAll()
        {
            Generate(null, GenerateComponents, GenerateCanonicalConstructors);
            Generate("Constructors", GenerateFurtherConstructors);
            Generate("Swizzles", GenerateSomeSwizzles);
            Generate("Operators", GenerateOperators);
            Generate("UnitProperties", GenerateUnitProperties);
            Generate("Casts", GenerateCasts);
            if (componentType == "float")
                Generate("Methods", GenerateLerp, GenerateDot, GenerateLength, GenerateReflect, GenerateClamp);
            Generate("ObjectMethods", GenerateObjectMethods);
        }

        private void GenerateComponents()
        {
            for (int i = 0; i < numComponents; i++)
            {
                WriteSummary($"The {componentNames[i]} component of the vector.");
                codeBuilder.WriteLine($"public {componentType} {componentNames[i]};");
            }
        }

        private void GenerateCanonicalConstructors()
        {
            WriteSummary($"Creates a new instance of {See(typeName)}.");
            var parameters = string.Join(", ", components.Select(X => $"{componentType} {X.ToLower()}"));
            codeBuilder.WriteLine($"public {typeName}({parameters})");
            using (codeBuilder.Block())
            {
                foreach (var X in components)
                {
                    codeBuilder.WriteLine($"this.{X} = {X.ToLower()};");
                }
            }
        }

        private void GenerateFurtherConstructors()
        {
            WriteSummary($"Creates a new instance of {See(typeName)} with all components initialized to the specified value.");
            codeBuilder.WriteLine($"public {typeName}({componentType} value)");
            codeBuilder.WriteLine($" => {string.Join(" = ", components)} = value;");

            for (int firstVectorLength = 2; firstVectorLength < numComponents; firstVectorLength++)
            {
                List<string> parameters = new List<string>();
                string firstVectorType = $"{baseName}{firstVectorLength}";
                string firstVectorParam = firstVectorType.ToLower();
                parameters.Add($"{firstVectorType} {firstVectorParam}");
                for (int i = firstVectorLength; i < numComponents; i++)
                {
                    parameters.Add($"{componentType} {componentNames[i].ToLower()}");
                }

                WriteSummary($"Creates a new instance of {See(typeName)}.");
                var joinedParameters = string.Join(", ", parameters);
                codeBuilder.WriteLine($"public {typeName}({joinedParameters})");
                using (codeBuilder.Block())
                {
                    for (int i = 0; i < numComponents; i++)
                    {
                        if (i < firstVectorLength)
                        {
                            codeBuilder.WriteLine($"{componentNames[i]} = {firstVectorParam}.{componentNames[i]};");
                        }
                        else
                        {
                            codeBuilder.WriteLine($"{componentNames[i]} = {componentNames[i].ToLower()};");
                        }
                    }
                }
            }
        }

        private void GenerateSomeSwizzles()
        {
            foreach (var swizzle in swizzles)
            {
                GenerateSwizzle(swizzle.Select(c => c.ToString()).ToArray());
            }
        }

        private void GenerateAllSwizzles()
        {
            int maxComponentCount = 4;
            for (int swizzleLength = 2; swizzleLength <= maxComponentCount; swizzleLength++)
            {
                string[] swizzledComponents = new string[swizzleLength];
                int combinations = (int)Math.Pow(numComponents, swizzleLength);
                for (int combination = 0; combination < combinations; combination++)
                {
                    int combination_rest = combination;
                    for (int swizzleIndex = swizzleLength - 1; swizzleIndex >= 0; swizzleIndex--)
                    {
                        swizzledComponents[swizzleIndex] = componentNames[combination_rest % numComponents];
                        combination_rest /= numComponents;
                    }
                    GenerateSwizzle(swizzledComponents);
                }
            }
        }

        private void GenerateSwizzle(string[] components)
        {
            string swizzle = string.Join("", components);
            string swizzleType = $"{baseName}{components.Length}";
            string swizzleParams = string.Join(", ", components);
            string humanReadableSwizzle = string.Join(", ", components.Select(See).Take(components.Length - 1)) +
                " and " + components.Select(See).Last();

            WriteSummary($"Creates a new {See(swizzleType)} consisting of the {humanReadableSwizzle} components of this {See(typeName)}.");
            //codeBuilder.WriteLine($"[DebuggerHidden, EditorBrowsable(EditorBrowsableState.Never), DebuggerBrowsableState(DebuggerBrowsableState.Never)]");
            codeBuilder.WriteLine($"public readonly {swizzleType} {swizzle} => new {swizzleType}({swizzleParams});");
        }

        private void GenerateOperators()
        {
            foreach (var op in supportedBinaryOperators)
            {
                string escapedOp = System.Security.SecurityElement.Escape(op);
                WriteSummary($"Applies the <code>{escapedOp}</code>-operator component-wise.");
                codeBuilder.WriteLine($"public static {typeName} operator {op}({VectorParam("left")}, {VectorParam("right")})");
                codeBuilder.WriteLine($" => {New(X => $"left.{X} {op} right.{X}")};");

                WriteSummary($"Applies the <code>{escapedOp}</code>-operator component-wise.");
                codeBuilder.WriteLine($"public static {typeName} operator {op}({VectorParam("left")}, {componentType} right)");
                codeBuilder.WriteLine($" => {New(X => $"left.{X} {op} right")};");

                WriteSummary($"Applies the <code>{escapedOp}</code>-operator component-wise.");
                codeBuilder.WriteLine($"public static {typeName} operator {op}({componentType} left, {VectorParam("right")})");
                codeBuilder.WriteLine($" => {New(X => $"left {op} right.{X}")};");

                foreach (var (upcastedComponent, upcastedVector) in binaryComponentUpcasts)
                {
                    WriteSummary($"Applies the <code>{escapedOp}</code>-operator component-wise.");
                    codeBuilder.WriteLine($"public static {upcastedVector} operator {op}({VectorParam("left")}, {upcastedComponent} right)");
                    codeBuilder.WriteLine($" => {New(X => $"left.{X} {op} right", upcastedVector)};");

                    WriteSummary($"Applies the <code>{escapedOp}</code>-operator component-wise.");
                    codeBuilder.WriteLine($"public static {upcastedVector} operator {op}({upcastedComponent} left, {VectorParam("right")})");
                    codeBuilder.WriteLine($" => {New(X => $"left {op} right.{X}", upcastedVector)};");
                }
            }

            foreach (var op in supportedUnaryOperators)
            {
                WriteSummary($"Applies the <code>{op}</code>-operator component-wise.");
                codeBuilder.WriteLine($"public static {typeName} operator {op}({VectorParam("vector")})");
                codeBuilder.WriteLine($" => {New(X => $"{op}vector.{X}")};");
            }


            WriteSummary($"Checks two {See(typeName)}s for equality.");
            codeBuilder.WriteLine($"public static bool operator ==({VectorParam("left")}, {VectorParam("right")})");
            codeBuilder.WriteLine($" => {string.Join(" && ", components.Select(X => $"left.{X} == right.{X}"))};");

            WriteSummary($"Checks two {See(typeName)}s for inequality.");
            codeBuilder.WriteLine($"public static bool operator !=({VectorParam("left")}, {VectorParam("right")})");
            codeBuilder.WriteLine($" => !(left == right);");
        }

        private void GenerateUnitProperties()
        {
            foreach (var (propertyName, constant) in this.unitProperties)
            {
                WriteSummary($"Returns a {See(typeName)} where all components have the value <code>{constant}</code>.");
                codeBuilder.WriteLine($"public static readonly {typeName} {propertyName}");
                codeBuilder.WriteLine($" = {New(X => constant)};");
            }

            foreach (var component in components)
            {
                WriteSummary($"Returns a {See(typeName)} where the {component} component is {positiveConstant}, and all other components are {negativeConstant}.");
                codeBuilder.WriteLine($"public static readonly {typeName} Unit{component}");
                codeBuilder.WriteLine($" = {New(X => X == component ? positiveConstant : negativeConstant)};");
            }
        }

        private void GenerateCasts()
        {
            foreach (var cast in casts)
            {
                WriteSummary($"Returns a {See(cast.vectorType)} where all components are casted to {cast.componentType}.");
                codeBuilder.WriteLine($"public static {(cast.isImplicit ? "implicit" : "explicit")} operator {cast.vectorType}({VectorParam("vector")})");
                codeBuilder.WriteLine($" => {New(X => $"({cast.componentType})vector.{X}", cast.vectorType)};");
            }
            foreach (var cast in foreignCasts)
            {
                WriteSummary($"Returns a {See(typeName)} where all components are casted to {componentType}.");
                codeBuilder.WriteLine($"public static explicit operator {typeName}({VectorParam("vector", cast)})");
                codeBuilder.WriteLine($" => {New(X => $"({componentType})vector.{X}")};");
            }
            if(componentType == "bool")
            {
                foreach (var vectorBaseType in new[] { "Int", "Vector" })
                {
                    string vectorType = $"{vectorBaseType}{numComponents}";
                    WriteSummary($"Returns a {See(vectorType)} where each component is 1 if the respective component is true in this {See(typeName)}, and 0 otherwise.");
                    codeBuilder.WriteLine($"public readonly {vectorType} As{vectorBaseType}()");
                    codeBuilder.WriteLine($" => {New(X => $"{X} ? 1 : 0", vectorType)};");
                }
            }
        }

        private void GenerateLerp()
        {
            WriteSummary($"Linearly interpolates between two {See(typeName)}s.");
            codeBuilder.WriteLine($"public static {typeName} Lerp({VectorParam("left")}, {VectorParam("right")}, {componentType} amount)");
            codeBuilder.WriteLine($" => left * (1 - amount) + right * amount;");
        }

        private void GenerateDot()
        {
            WriteSummary($"Computes the dot product between two {See(typeName)}s.");
            codeBuilder.WriteLine($"public static {componentType} Dot({VectorParam("left")}, {VectorParam("right")})");
            codeBuilder.WriteLine($" => {string.Join(" + ", components.Select(X => $"left.{X} * right.{X}"))};");
        }

        private void GenerateLength()
        {
            WriteSummary($"Gets the squared length of this {See(typeName)}.");
            codeBuilder.WriteLine($"public readonly {componentType} LengthSquared()");
            codeBuilder.WriteLine($" => Dot(this, this);");

            WriteSummary($"Gets the length of this {See(typeName)}.");
            codeBuilder.WriteLine($"public readonly {componentType} Length()");
            codeBuilder.WriteLine($" => MathF.Sqrt(LengthSquared());");

            WriteSummary($"Normalizes this {See(typeName)}. After the operation, this {See(typeName)} will have unit length.");
            codeBuilder.WriteLine($"public void Normalize()");
            codeBuilder.WriteLine($" => this /= Length();");

            WriteSummary($"Gets a normalized version of this {See(typeName)}.");
            codeBuilder.WriteLine($"public readonly {typeName} Normalized()");
            codeBuilder.WriteLine($" => this / Length();");
        }

        private void GenerateReflect()
        {
            WriteSummary($"Reflects this {See(typeName)} off a surface with the specified normal.");
            codeBuilder.WriteLine($"public static {typeName} Reflect({VectorParam("vector")}, {VectorParam("normal")})");
            codeBuilder.WriteLine($" => vector - 2 * Dot(vector, normal) * normal;");
        }

        private void GenerateClamp()
        {
            WriteSummary($"Clamps all components of a {See(typeName)} to the specified minima and maxima.");
            codeBuilder.WriteLine($"public static {typeName} Clamp({VectorParam("vector")}, {componentType} min, {componentType} max)");
            codeBuilder.WriteLine($" => {New(X => $"Math.Min(Math.Max(vector.{X}, min), max)")};");

            WriteSummary($"Clamps all components of a {See(typeName)} to the specified minima and maxima.");
            codeBuilder.WriteLine($"public static {typeName} Clamp({VectorParam("vector")}, {VectorParam("min")}, {VectorParam("max")})");
            codeBuilder.WriteLine($" => {New(X => $"Math.Min(Math.Max(vector.{X}, min.{X}), max.{X})")};");

            WriteSummary($"Clamps all components of a {See(typeName)} between 0 and 1.");
            codeBuilder.WriteLine($"public static {typeName} Clamp({VectorParam("vector")})");
            codeBuilder.WriteLine($" => {New(X => $"Math.Min(Math.Max(vector.{X}, 0), 1)")};");
        }

        private void GenerateObjectMethods()
        {
            WriteSummary($"Creates a string representation for this {See(typeName)}.");
            codeBuilder.WriteLine($"public readonly override string ToString()");
            codeBuilder.WriteLine($" => $\"<{string.Join(" ", components.Select(X => $"{{{X}}}"))}>\";");

            WriteSummary($"Checks whether this {See(typeName)} is equivalent to another.");
            codeBuilder.WriteLine($"public readonly override bool Equals(object other)");
            codeBuilder.WriteLine($" => other is {typeName} otherVector ");
            codeBuilder.WriteLine($" && {string.Join(" && ", components.Select(X => $"{X} == otherVector.{X}"))};");

            WriteSummary($"Generates a hash code for this {See(typeName)}.");
            codeBuilder.WriteLine($"public readonly override int GetHashCode()");
            codeBuilder.WriteLine($" => {string.Join(" ^ ", components.Select((X, i) => $"({X}.GetHashCode() << {i})"))};");
        }
    }
}
