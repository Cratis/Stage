// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_local_variable_is_named_java : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    ImmutableArray<DeclarativeFileResolution> _decimal;
    ImmutableArray<DeclarativeFileResolution> _inherited;
    ImmutableArray<DeclarativeFileResolution> _planType;
    ImmutableArray<DeclarativeFileResolution> _claimed;

    string Gap() => _pack.Placeholder(GapRole.DenyEnclosingOperation, "C", "i");

    void Because()
    {
        _result = Resolve(Class("example", "A", Txt("int java = 0;\n" + Gap() + "\n")));
        _decimal = Resolve(Class("example", "B", Txt("int java = 0;\nvar d = " + _pack.Decimal(1.5m) + ";\n")));
        _inherited = Resolve(Class("example", "C", Reference("Javaish", "lib"), Txt(" j;\n" + Gap() + "\n")));
        _planType = Resolve(Class("example", "java"), Class("example", "D", Txt(Gap() + "\n")));
        _claimed = Resolve(Class("example", "java"), Class("example", "E", Reference("java"), Txt(" j;\n" + Gap() + "\n")));
    }

    [Fact] void should_refuse_a_local_variable_with_a_placeholder() => Code(_result[0]).ShouldEqual(JavaRefusalCodes.LiteralShadowed);
    [Fact] void should_refuse_a_local_variable_with_a_decimal_literal() => Code(_decimal[0]).ShouldEqual(JavaRefusalCodes.LiteralShadowed);
    [Fact] void should_refuse_an_inherited_field() => Code(_inherited[0]).ShouldEqual(JavaRefusalCodes.LiteralShadowed);
    [Fact] void should_refuse_a_same_package_plan_type() => Code(_planType[1]).ShouldEqual(JavaRefusalCodes.LiteralShadowed);
    [Fact] void should_refuse_a_short_claim() => Code(_claimed[1]).ShouldEqual(JavaRefusalCodes.LiteralShadowed);
}
