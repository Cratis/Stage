// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_java_lang_is_shadowed_by_a_plan_type_while_used_literally : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Txt("String x = \"\";\n"), Reference("String"), Txt(" a;\n")), Class("example", "String"));

    [Fact] void should_refuse_the_literal_use() => Code(_result[0]).ShouldEqual(JavaRefusalCodes.LiteralShadowed);
}
