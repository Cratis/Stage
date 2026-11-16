// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

// An adopted screen needs data, and the adopting slice provides none of it: Screenplay's executable screen admission refuses it with a reason naming the screen
// and the shape, so Stage plans nothing for the model rather than rendering a screen with an empty table (Cratis/Stage#1150).
public class when_planning_an_adopted_free_screen_with_an_unprovided_need : Specification
{
    const string Source = """
        shape Book
          title String

        screen BookCatalog
          data Book[]
          table Book
            column title

        module Lending
          feature Catalog
            slice StateChange AddBook
              command AddBook
                bookId Uuid identifier
                title String
                produces BookAdded
                  for bookId
                  title = title
              event BookAdded
                title String

            slice StateView Books
              readmodel CatalogEntry
                isbn Uuid key
                title String
              projection CatalogProjection => CatalogEntry
                from BookAdded
                  isbn = $eventSourceId
                  title = title
              query AllBooks => CatalogEntry[]
              uses screen BookCatalog
        """;

    CratisPlanResult _result = null!;
    string _root = null!;

    async Task Because()
    {
        _root = Path.Combine(Path.GetTempPath(), $"stage-unprovided-need-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(Path.Combine(_root, "lending.play"), Source);
        _result = await CratisRendering.PlanFrom(new PlaySources(_root, []), new PlanSelection([PlanSelectionEntry.Module("Lending")]), new CratisPlanOptions("Lending", "Lending", "Lending"));
    }

    void Destroy() => Directory.Delete(_root, recursive: true);

    [Fact] void should_not_plan_the_model() => _result.Success.ShouldBeFalse();
    [Fact] void should_plan_no_artifacts() => _result.Artifacts.ShouldBeEmpty();
    [Fact] void should_name_the_screen_and_the_shape_in_the_reason() => string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)).ShouldContain("Screen 'BookCatalog' adopted by 'Lending.Catalog.Books' needs 'Book' of shape 'Book', and slice 'Books' does not provide it.");
    [Fact] void should_report_it_as_a_free_screen_refusal() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-SCENE-FREE-001");
}
