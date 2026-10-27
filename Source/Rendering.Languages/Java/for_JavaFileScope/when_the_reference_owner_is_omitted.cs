// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope;

public class when_the_reference_owner_is_omitted : given.a_file_scope
{
    void Establish() => CreateScope([new("java.lang", "String", []), new("example", "Order", []), new("library", "Widget", [])]);

    [Fact] void should_not_infer_the_file_declaration_as_the_owner() => _scope.Spell(new("java.lang", "String", [])).ShouldEqual("java.lang.String");
    [Fact] void should_not_shorten_same_package_types() => _scope.Spell(new("example", "Order", [])).ShouldEqual("example.Order");
    [Fact] void should_not_shorten_imported_types() => _scope.Spell(new("library", "Widget", [])).ShouldEqual("library.Widget");
    [Fact] void should_still_shorten_in_a_proven_explicit_scope() => _scope.Spell(new("java.lang", "String", []), "container").ShouldEqual("String");
}
