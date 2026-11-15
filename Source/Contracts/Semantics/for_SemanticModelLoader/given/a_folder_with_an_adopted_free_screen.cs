// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

/// <summary>
/// A folder holding a model with a free screen that a slice adopts; a derived specification states what the adopting slice provides.
/// </summary>
public abstract class a_folder_with_an_adopted_free_screen : Specification
{
    protected string _root = null!;
    protected SemanticModelLoadResult _result = null!;

    /// <summary>
    /// Gets the lines the adopting slice states after <c language="screenplay">uses screen BookCatalog</c>.
    /// </summary>
    protected abstract string Provision { get; }

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), $"stage-free-screen-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "lending.play"), $$"""
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
            {{Provision}}
            """);
    }

    void Destroy() => Directory.Delete(_root, recursive: true);
}
