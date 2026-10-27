// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations;

public class when_a_parent_is_missing : given.a_declaration_plan
{
    Exception _error = null!;

    void Establish() => _plan = new([new("file", "example", [new DeclarationSegment(new("example", "Outer", ["Child"]), "child")])], []);
    void Because() => _error = Catch.Exception(() => _declarations = new(_plan));

    [Fact] void should_refuse_on_the_parent_binding_path() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.MissingDeclaration);
}
