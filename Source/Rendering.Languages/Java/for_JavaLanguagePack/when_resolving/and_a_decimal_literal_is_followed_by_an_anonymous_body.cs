// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_decimal_literal_is_followed_by_an_anonymous_body : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = new JavaLanguagePack(JavaTypeFacts.Parse("""{"types":{"lib.ZERO":[]}}""")).Resolve(
        [Class("example", "A", Txt("Object o = " + _pack.Decimal(2m) + " {\n    Object y = "), Reference("ZERO", "lib"), Txt(".valueOf(2);\n  };\n"))]);

    [Fact]
    void should_qualify_a_name_the_big_decimal_body_inherits() =>
        Text(_result[0]).ShouldEqual(Expected("A", "Object o = new java.math.BigDecimal(\"2\") {\n    Object y = lib.ZERO.valueOf(2);\n  };\n"));
}
