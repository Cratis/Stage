// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_two_spellings_are_adjacent : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Reference("Thing", "lib"), Reference("Thing", "other"), Txt(" f;\n")));

    [Fact] void should_refuse_spellings_that_would_merge() => Code(_result[0]).ShouldEqual(JavaRefusalCodes.SpellingContext);
}
