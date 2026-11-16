// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

// Screenplay's executable screen admission refuses a screen whose need no query of the adopting slice provides; Stage serves nothing for the model rather
// than a screen without data, in the live Stage and in a generated application alike (Cratis/Stage#1150).
public class when_loading_an_adopted_free_screen_with_an_unprovided_need : given.a_folder_with_an_adopted_free_screen
{
    protected override string Provision => string.Empty;

    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, [], null, "Lending");

    [Fact] void should_refuse_the_model() => _result.Success.ShouldBeFalse();
    [Fact] void should_refuse_it_as_a_free_screen() => _result.Errors.Select(error => error.Code).ShouldContain(SemanticModelLoader.FreeScreenRefusalCode);
    [Fact] void should_give_the_reason_admission_gives() => _result.Errors.Select(error => error.Message).ShouldContain("Screen 'BookCatalog' adopted by 'Lending.Catalog.Books' needs 'Book' of shape 'Book', and slice 'Books' does not provide it.");
}
