// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations;

public class when_external_member_metadata_is_duplicated : given.a_declaration_plan
{
    Exception _error = null!;

    void Establish() => _plan = new([], []) { ExternalDeclarations = [new(new("lib", "Base", []), KnownTypes<string>.None), new(new("lib", "Base", []))] };
    void Because() => _error = Catch.Exception(() => _declarations = new(_plan));

    [Fact] void should_not_choose_a_first_metadata_winner() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.AmbiguousDeclaration);
}
