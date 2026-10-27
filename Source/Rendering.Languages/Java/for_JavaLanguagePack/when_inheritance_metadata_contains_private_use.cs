// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_inheritance_metadata_contains_private_use : given.a_symbol_plan
{
    Exception[] _errors = [];

    void Because()
    {
        var invalid = new TypeName("lib", "Invalid\uE000", []);
        SymbolPlan[] plans =
        [
            new([], []) { ExternalDeclarations = [new(invalid, KnownTypes<string>.None)] },
            new([], []) { ExternalDeclarations = [new(new("lib", "Base", []), KnownTypes<string>.Of(["Invalid\uE000"]))] },
            new([], []) { ExternalDeclarations = [new(new("lib", "Base", []), KnownTypes<string>.None) { Supertypes = KnownTypes<TypeName>.Of([invalid]) }] },
            new([new("file", "p", [new DeclarationSegment(new("p", "C", []), "C") { Supertypes = KnownTypes<TypeName>.Of([invalid]) }])], []),
        ];
        _errors = [.. plans.Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];
    }

    [Fact] void should_validate_every_new_metadata_field() => _errors.ShouldEachConformTo(error => ((LanguagePackRefusal)error).Code == JavaRefusalCodes.PrivateUseCodePoint);
}
