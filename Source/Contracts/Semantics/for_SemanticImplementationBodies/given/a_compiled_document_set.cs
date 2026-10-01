// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticImplementationBodies.given;

public class a_compiled_document_set : Specification
{
    protected const string InlineSource =
        """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                amount Decimal
                validate csharp
                  ```csharp
                  if (context.Artifact.amount <= 0) yield return "Nothing to order";
                  ```
                produces OrderPlaced
                  orderId = orderId
                  amount = amount
              event OrderPlaced
                orderId Uuid
                amount Decimal
        """;

    protected const string FileSource =
        """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                amount Decimal
                validate
                  amount rule Positive message "Positive amount required"
                    file Rules/Positive.cs
                produces OrderPlaced
                  orderId = orderId
                  amount = amount
              event OrderPlaced
                orderId Uuid
                amount Decimal
        """;

    protected SemanticDocumentSet _documents = null!;
    protected ImmutableArray<SemanticImplementationRequirement> _requirements;

    /// <summary>
    /// Compiles one in-memory document under a stable key that is deliberately not the loader's hex-encoded path.
    /// </summary>
    /// <param name="source">The Screenplay source.</param>
    /// <param name="attachments">The attachment contents keyed by portable path.</param>
    protected void Compile(string source, ImmutableDictionary<string, string>? attachments = null)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("orders-document"), "orders-document", "Orders.play", source);
        _documents = SemanticDocumentSet.Create([document], catalog, attachments);
        var compiled = new SemanticModelCompiler().Compile("Orders", _documents);
        if (!compiled.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        _requirements = compiled.ImplementationRequirements;
    }
}
