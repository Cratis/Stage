// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_an_external_type_has_no_facts : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Reference("Type", "unknown"), Txt(" t;\n")), Class("example", "B"));

    [Fact] void should_refuse_the_file_that_references_it() => Code(_result[0]).ShouldEqual(JavaRefusalCodes.UnknownType);
    [Fact] void should_resolve_the_other_files() => Text(_result[1]).ShouldEqual(Expected("B", string.Empty));
}
