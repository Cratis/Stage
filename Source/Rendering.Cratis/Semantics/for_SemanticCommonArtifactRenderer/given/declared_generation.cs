// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommonArtifactRenderer.given;

internal static class declared_generation
{
    // None of these declarations is used as an identifier or a generated command property.
    const string Source = """
        concept StringUuid : String
          generate uuid
        concept Sortable : String
          generate uuidv7 case upper prefix "evt-"
        concept UlidRef : String
          generate ulid case lower
        concept PersonId : String
          generate typeid "person"
        concept ShortCode : String
          generate nanoid 8 alphabet "ABC123"
        concept HexReference : String
          generate bytes 16 hex case upper
        concept Base32Reference : String
          generate bytes 16 base32 case lower
        concept UrlReference : String
          generate bytes 16 base64url
        concept CheckedReference : String
          generate pattern "#{16}" checksum luhn
        concept DatedReference : String
          generate pattern "{yyyy}-{MM}-{dd}-?{16}"
        concept TicketNumber : Int
          generate random -9223372036854775808..9223372036854775807
        concept TenantOrderId : String
          generate rule TenantOrder
            ```csharp
            return "Generated";
            ```
        concept New : String
          generate nanoid
        concept OrdinaryText : String
        concept OrdinaryUuid : Uuid
        module Shop
          feature Orders
            slice StateChange Create
              command Create
                streamId Uuid identifier
                produces Created
                  for streamId
              event Created
        """;

    internal static SemanticApplicationContext Context(string body = "return \"Generated\";")
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop"));
        var source = Source.Replace("return \"Generated\";", body.Replace("\n", "\n    ", StringComparison.Ordinal), StringComparison.Ordinal);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("shop"), "shop", "Shop.play", source);
        var compiled = new SemanticModelCompiler().Compile("Shop", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Diagnostics));
        var model = compiled.Value.Model;
        var options = new CratisRenderingOptions("Shop", "Shop");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, CratisRendering.CreateProfile("Shop", options), new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = compiled.ImplementationRequirements,
            ImplementationContents = SemanticImplementationBodies.Resolve(compiled.Value.Documents, compiled.ImplementationRequirements),
            TypedContextDescriptors = compiled.TypedContextDescriptors
        };
        return new(request, options);
    }
}
