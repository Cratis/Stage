// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations;

public class when_a_declaration_is_missing : given.a_declaration_plan
{
    Exception _error = null!;

    void Establish() => _declarations = new(new([], []));
    void Because() => _error = Catch.Exception(() => _declarations.Declared("missing", new(new("example", "Missing", []), "owner")));

    [Fact] void should_refuse_instead_of_inventing_a_name() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.MissingDeclaration);
}
