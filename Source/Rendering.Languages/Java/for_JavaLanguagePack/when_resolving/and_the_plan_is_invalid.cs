// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_the_plan_is_invalid : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A"), Class("example", "B"), File("sub/A.java", Imports(), Txt("public class "), Declaration("A"), Txt(" {}\n")));

    [Fact] void should_refuse_every_file_with_the_plan_wide_code() => _result.Select(Code).ShouldEachConformTo(code => code == JavaRefusalCodes.DuplicateDeclaration);
}
