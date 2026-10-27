// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_the_type_is_in_the_same_package : given.a_file_scope
{
    void Establish() => _file = _file with { Segments = [new DeclarationSegment(new("example", "Order", []), "order")] };
    void Because() => CreateScope(new("example", "Order", []), new("elsewhere", "Order", []));

    [Fact] void should_shorten_the_same_package_type() => _scope.Spell(new("example", "Order", [])).ShouldEqual("Order");
    [Fact] void should_qualify_the_foreign_type() => _scope.Spell(new("elsewhere", "Order", [])).ShouldEqual("elsewhere.Order");
    [Fact] void should_not_import_a_shadowed_type() => _scope.Imports.Count.ShouldEqual(0);
}
