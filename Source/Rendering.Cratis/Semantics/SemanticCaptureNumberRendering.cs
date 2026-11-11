// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Emits the frozen .NET 10 conversion as local functions, without a Stage runtime dependency or capture member names.
/// </summary>
internal static class SemanticCaptureNumberRendering
{
    internal static void Render(CSharpCodeBuilder builder)
    {
        builder.Line("// Frozen .NET 10 conversion: runtime decimal casts differ on .NET 11.")
            .OpenBlock("static decimal Number(double input, int precision)")
            .Line("var exponent = (int)((global::System.BitConverter.DoubleToUInt64Bits(input) >> 52) & 0x7FF) - 1022;")
            .Line("if (exponent < -94) return 0;")
            .Line("if (exponent > 96) throw new global::System.OverflowException(\"Value was either too large or too small for a Decimal.\");")
            .Line("var (mantissa, scale) = ScaleNumber(global::System.Math.Abs(input), exponent, precision);")
            .Line("return mantissa == 0 ? 0 : BuildNumber(mantissa, scale, input < 0, precision);")
            .EndBlock().BlankLine();
        RenderScale(builder);
        RenderDecimal(builder);
    }

    static void RenderScale(CSharpCodeBuilder builder) =>
        builder.OpenBlock("static (ulong Mantissa, int Scale) ScaleNumber(double input, int exponent, int precision)")
            .Line("global::System.ReadOnlySpan<double> powers = [1, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22, 1e23, 1e24, 1e25, 1e26, 1e27, 1e28];")
            .Line("var scale = precision - ((exponent * 19728) >> 16);")
            .OpenBlock("if (scale >= 0)")
            .Line("scale = global::System.Math.Min(scale, 28);")
            .Line("input *= powers[scale];")
            .EndBlock()
            .OpenBlock("else if (scale != -1 || input >= powers[precision + 1])")
            .Line("input /= powers[-scale];")
            .EndBlock()
            .OpenBlock("else")
            .Line("scale = 0;")
            .EndBlock()
            .OpenBlock("if (input < powers[precision] && scale < 28)")
            .Line("input *= 10;")
            .Line("scale++;")
            .EndBlock()
            .Line("return ((ulong)global::System.Math.Round(input, global::System.MidpointRounding.ToEven), scale);")
            .EndBlock().BlankLine();

    static void RenderDecimal(CSharpCodeBuilder builder) =>
        builder.OpenBlock("static decimal BuildNumber(ulong mantissa, int scale, bool negative, int precision)")
            .OpenBlock("if (scale < 0)")
            .Line("var result = (decimal)mantissa;")
            .Line("for (var index = 0; index < -scale; index++) result *= 10;")
            .Line("return negative ? -result : result;")
            .EndBlock()
            .Line("var removable = global::System.Math.Min(scale, precision);")
            .OpenBlock("while (removable > 0 && mantissa % 10 == 0)")
            .Line("mantissa /= 10;")
            .Line("scale--;")
            .Line("removable--;")
            .EndBlock()
            .Line("return new decimal((int)(uint)mantissa, (int)(uint)(mantissa >> 32), 0, negative, (byte)scale);")
            .EndBlock().BlankLine();
}
