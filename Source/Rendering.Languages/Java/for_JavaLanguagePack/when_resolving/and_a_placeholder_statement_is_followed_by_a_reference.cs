// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_placeholder_statement_is_followed_by_a_reference : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = new JavaLanguagePack(JavaTypeFacts.Parse("""{"types":{"lib.ZERO":[],"java.lang.UnsupportedOperationException":["ZERO"]}}""")).Resolve(
        [Class("example", "A", Txt("void f() { " + _pack.Placeholder(GapRole.ThrowingOperationBody, "C", "intent") + " }\n  "), Reference("ZERO", "lib"), Txt(" z;\n"))]);

    [Fact]
    void should_treat_the_throw_target_as_a_literal_type_whose_members_may_be_inherited() =>
        Text(_result[0]).ShouldEqual(Expected("A", "void f() { throw new java.lang.UnsupportedOperationException(\"C: intent\"); }\n  lib.ZERO z;\n"));
}
