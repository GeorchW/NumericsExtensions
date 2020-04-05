NumericsExtensions
==================

This repository contains some useful extensions to the default System.Numerics namespace. In particular, it adds the following:

- `Int2`, `Int3`, `Int4`, `Bool2`, `Bool3`, `Bool4`
  - Including the usual casts, component-wise operators etc.
  - Also including the most common swizzles
- The most common swizzles as extension methods to the `System.Numerics.Vector*` types

Building
--------

Note: Building the project was only tested on Windows. As it uses Dotnetcore, it should be straightfoward to run it on Linux as well.

1. Start Visual Studio
2. Press F5 to generate the missing types
3. Press F6 to compile the project itself.

FAQ
---

### Why are there only Int and Bool types, and not Short/Long/Double/Decimal/...?

I tried to only include types that are actually used, based on my personal experience. More types just mean more corner cases, and the number of possible casts is increasing quadratically, deteriorating editor performance.

### Why are only some swizzles included?

There are exponentially many swizzles. Most of them are never used, for example, I've never seen `XWYW` in the wild. IntelliSense is not built for that many members, and even when they are hidden, the performance burden is still unneccessary. 

Also, all kinds of reflection tools that try to list all members are constantly hanging when there are thousands of them. For the types included in this library, which I expect to be used very commonly, this is not particularly desirable.

### Why isn't this library providing its own Vector* types, and instead relies on extension methods?

The support for the BCL types is likely much better. This improves reliability and performance. Additionally, the BCL types and many of their methods are recognized as Intrinsics by the runtime, further increasing performance.

If you want to, you can uncomment the respective lines in `Program.cs` as a starting point to generate Vector types as well.

Changelog
---------

### 0.2.0

Add implicitely converting operators (`Int2 * float -> Vector2` etc.)

### 0.1.0

Initial version
