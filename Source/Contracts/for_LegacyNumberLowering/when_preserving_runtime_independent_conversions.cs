// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_LegacyNumberLowering;

public class when_preserving_runtime_independent_conversions : Specification
{
    [Theory]
    [InlineData(9007199254740991d, "9007199254740990")]
    [InlineData(1.23d, "1.23")]
    [InlineData(-1.23d, "-1.23")]
    [InlineData(0.1d, "0.1")]
    [InlineData(123456789012345d, "123456789012345")]
    [InlineData(1234567890123456d, "1234567890123460")]
    [InlineData(1e-28d, "0.0000000000000000000000000001")]
    [InlineData(1e28d, "10000000000000000000000000000")]
    [InlineData(1e-100d, "0")]
    [InlineData(0d, "0")]
    [InlineData(1.234567890123445d, "1.23456789012344")]
    public void should_keep_the_double_conversion(double value, string expected)
    {
        LegacyNumberLowering.FromDouble(value).ShouldEqual(decimal.Parse(expected, CultureInfo.InvariantCulture));
#if NET10_0
        LegacyNumberLowering.FromDouble(value).ShouldEqual((decimal)value);
#endif
    }

    [Theory]
    [InlineData(1.23f, "1.23")]
    [InlineData(0.1f, "0.1")]
    [InlineData(-1.23f, "-1.23")]
    [InlineData(12345678f, "12345680")]
    [InlineData(1e-28f, "0.0000000000000000000000000001")]
    [InlineData(1e28f, "9999999000000000000000000000")]
    [InlineData(float.Epsilon, "0")]
    public void should_keep_the_single_conversion(float value, string expected)
    {
        LegacyNumberLowering.FromSingle(value).ShouldEqual(decimal.Parse(expected, CultureInfo.InvariantCulture));
#if NET10_0
        LegacyNumberLowering.FromSingle(value).ShouldEqual((decimal)value);
#endif
    }

    [Theory]
    [InlineData(1e29d)]
    [InlineData(-1e29d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void should_reject_unrepresentable_doubles(double value) =>
        Catch.Exception(() => LegacyNumberLowering.FromDouble(value)).ShouldBeOfExactType<OverflowException>();

    [Theory]
    [InlineData(float.MaxValue)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void should_reject_unrepresentable_singles(float value) =>
        Catch.Exception(() => LegacyNumberLowering.FromSingle(value)).ShouldBeOfExactType<OverflowException>();

    [Fact] void should_keep_decimal_precision() => LegacyNumberLowering.From(1.2345678901234567890123456789m).ShouldEqual(1.2345678901234567890123456789m);
    [Fact] void should_keep_integer_precision() => LegacyNumberLowering.From(9007199254740991L).ShouldEqual(9007199254740991m);
    [Fact] void should_canonicalize_negative_zero() => decimal.GetBits(LegacyNumberLowering.FromDouble(-0d))[3].ShouldEqual(0);
    [Fact] void should_keep_the_double_scale() => decimal.GetBits(LegacyNumberLowering.FromDouble(1.23d))[3].ShouldEqual(2 << 16);
}
