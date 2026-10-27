// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class and_an_unrelated_type_is_added : given.a_declaration_plan
{
    JavaDeclarations _extended = null!;

    void Establish() => _plan = new([File("one", "Order", "first"), File("two", "Order", "second")], []);

    void Because()
    {
        _declarations = new(_plan);
        _extended = new(_plan with { Files = [.. _plan.Files, File("other", "Unrelated", "other")] });
    }

    [Fact] void should_preserve_every_existing_name() => _plan.Files.All(file => _declarations.InFile(file.Id).Single().Name == _extended.InFile(file.Id).Single().Name).ShouldBeTrue();
}
