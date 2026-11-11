// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Mirrors Screenplay's frozen .NET 10 floating-point to decimal conversion, independent of the runtime.
/// </summary>
// https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Decimal.DecCalc.cs
[SuppressMessage("Design", "MA0182", Justification = "Used by the host, Cratis renderer and template renderer through InternalsVisibleTo.")]
internal static class LegacyNumberLowering
{
    static ReadOnlySpan<double> PowersOfTen =>
    [
        1, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9,
        1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19,
        1e20, 1e21, 1e22, 1e23, 1e24, 1e25, 1e26, 1e27, 1e28
    ];

    internal static decimal From(object? value) => value switch
    {
        double number => FromDouble(number),
        float number => FromSingle(number),
        _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
    };

    internal static decimal FromDouble(double value) => FromFloatingPoint(value, 14);

    internal static decimal FromSingle(float value) => FromFloatingPoint(value, 6);

    static decimal FromFloatingPoint(double value, int digits)
    {
        var exponent = (int)((BitConverter.DoubleToUInt64Bits(value) >> 52) & 0x7FF) - 1022;
        if (exponent < -94) return 0;
        if (exponent > 96) throw new OverflowException("Value was either too large or too small for a Decimal.");

        var (mantissa, scale) = Scale(Math.Abs(value), exponent, digits);
        if (mantissa == 0) return 0;
        return ToDecimal(mantissa, scale, value < 0, digits);
    }

    static (ulong Mantissa, int Scale) Scale(double value, int exponent, int digits)
    {
        // Retain the original binary scaling: G15/G7 can round differently at decimal midpoints.
        var scale = digits - ((exponent * 19728) >> 16);
        if (scale >= 0)
        {
            scale = Math.Min(scale, 28);
            value *= PowersOfTen[scale];
        }
        else if (scale != -1 || value >= PowersOfTen[digits + 1])
        {
            value /= PowersOfTen[-scale];
        }
        else
        {
            scale = 0;
        }

        if (value < PowersOfTen[digits] && scale < 28)
        {
            value *= 10;
            scale++;
        }

        return ((ulong)Math.Round(value, MidpointRounding.ToEven), scale);
    }

    static decimal ToDecimal(ulong mantissa, int scale, bool negative, int digits)
    {
        if (scale < 0)
        {
            var result = (decimal)mantissa;
            for (var index = 0; index < -scale; index++) result *= 10;
            return negative ? -result : result;
        }

        // Preserve the old scale as well as the numeric value.
        var removable = Math.Min(scale, digits);
        while (removable > 0 && mantissa % 10 == 0)
        {
            mantissa /= 10;
            scale--;
            removable--;
        }

        return new decimal((int)(uint)mantissa, (int)(uint)(mantissa >> 32), 0, negative, (byte)scale);
    }
}
