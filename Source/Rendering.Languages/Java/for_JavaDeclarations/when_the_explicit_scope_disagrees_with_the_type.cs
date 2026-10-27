// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations;

public class when_the_explicit_scope_disagrees_with_the_type : given.a_declaration_plan
{
    Exception[] _errors = [];

    void Because() => _errors = [.. new[] { "Other", string.Empty }.Select(scope => Catch.Exception(() =>
        _declarations = new(new([new("file", "example", [new DeclarationSegment(new("example", "Outer", ["Child"]), "child", Scope: scope)])], []))))];

    [Fact] void should_refuse_nonmatching_and_explicitly_empty_scopes() => _errors.ShouldEachConformTo(error => ((LanguagePackRefusal)error).Code == JavaRefusalCodes.InvalidDeclarationScope);
}
