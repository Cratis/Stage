// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class and_two_names_sanitize_to_the_same_identifier : given.a_declaration_plan
{
    void Establish() => _plan = new([File("one", "a-b", "first"), File("two", "a_b", "second")], []);
    void Because() => _declarations = new(_plan);

    [Fact] void should_rename_the_sanitized_name() => _declarations.InFile("one").Single().Name.ShouldNotEqual("a_b");
    [Fact] void should_rename_the_valid_name() => _declarations.InFile("two").Single().Name.ShouldNotEqual("a_b");
    [Fact] void should_allocate_distinct_identifiers() => _declarations.InFile("one").Single().Name.ShouldNotEqual(_declarations.InFile("two").Single().Name);
}
