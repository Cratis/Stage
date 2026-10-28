// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_field_obscures_a_package_head : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    ImmutableArray<DeclarativeFileResolution> _inherited;

    void Because()
    {
        _result = Resolve(Class("example", "A", Txt("int com; int Y;\n"), Reference("Y", "com.x"), Txt(" y;\n")));
        _inherited = Resolve(Class("example", "B", Reference("Fields", "lib"), Txt(" f; int Y;\n"), Reference("Y", "com.x"), Txt(" y;\n")));
    }

    [Fact] void should_refuse_when_a_literal_field_has_the_package_name() => Code(_result[0]).ShouldEqual(JavaRefusalCodes.Unspellable);
    [Fact] void should_refuse_when_an_inherited_field_has_the_package_name() => Code(_inherited[0]).ShouldEqual(JavaRefusalCodes.Unspellable);
}
