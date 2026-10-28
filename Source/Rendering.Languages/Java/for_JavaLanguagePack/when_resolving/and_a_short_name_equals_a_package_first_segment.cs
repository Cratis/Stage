// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_short_name_equals_a_package_first_segment : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Reference("Item", "q"), Txt(" i;\n"), Reference("Y", "Item.z"), Txt(" y;\n")));

    [Fact] void should_not_shorten_the_name_that_a_package_head_uses() => Text(_result[0]).ShouldEqual(Expected("A", "q.Item i;\nY y;\n", "Item.z.Y"));
}
