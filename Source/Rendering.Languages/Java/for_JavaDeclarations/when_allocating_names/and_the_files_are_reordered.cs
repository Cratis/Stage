// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class and_the_files_are_reordered : given.a_declaration_plan
{
    JavaDeclarations _reordered = null!;

    void Establish() => _plan = new([File("one", "Order", "first"), File("two", "Order", "second")], []);

    void Because()
    {
        _declarations = new(_plan);
        _reordered = new(_plan with { Files = [.. _plan.Files.Reverse()] });
    }

    [Fact] void should_allocate_identical_names() => _plan.Files.All(file => _declarations.InFile(file.Id).Single().Name == _reordered.InFile(file.Id).Single().Name).ShouldBeTrue();
}
