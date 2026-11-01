// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;

internal static class handler_model
{
    internal const string Body = "return new OrderPlaced(context.Command.Name.ToUpperInvariant());";
    internal const string MultiBody = "var calls = (int?)global::System.AppContext.GetData(\"S1:\" + context.Command.Name) ?? 0; global::System.AppContext.SetData(\"S1:\" + context.Command.Name, calls + 1); var name = context.Command.Name + context.Command.Token.Value.ToString(); return (new OrderPlaced(name), new OrderEchoed(name));";
    internal const string MultiSource = """
        concept ReceiptToken : Uuid
          generate uuid
        module Orders
          feature Ordering
            slice StateChange Place
              command PlaceOrder
                orderId Uuid identifier
                name String
                token ReceiptToken generated
                validate
                  name not empty message "A name is required"
                handler
                  implementation
                    hint "Normalize the name"
                produces event OrderPlaced
                  for orderId
                  name String = name
                produces event OrderEchoed
                  for orderId
                  name String = name
        """;
    internal const string Source = """
        module Orders
          feature Ordering
            slice StateChange Place
              command PlaceOrder
                orderId Uuid identifier
                name String
                handler
                  implementation
                    hint "Normalize the name"
                produces event OrderPlaced
                  name String = name
        """;

    internal const string TimedBody = "return new OrderStamped(context.Command.Name.ToUpperInvariant(), global::System.DateTimeOffset.MinValue);";
    internal const string TimedCommand = """
              command TimedPlace
                orderId Uuid identifier
                name String
                handler
                  implementation
                    hint "Stamp the payload without owning envelope metadata"
                produces event OrderStamped
                  for orderId
                  name String = name
                  stamp DateTime = $context.occurred
        """;

    internal static string TimedSource => "module Orders\n  feature Ordering\n    slice StateChange Place\n" + TimedCommand;

    // Synthetic resolved attachment: the pinned compiler does not yet accept a handler body
    // beside produces. The model and requirement are compiler-owned; this envelope is not
    // evidence of native Screenplay body acceptance (Screenplay#1068).
    internal static ArtifactRenderRequest Request(string body = Body, string? source = null, IReadOnlyDictionary<string, string>? bodies = null)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("HandlerModel"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("orders"), "orders", "Orders.play", source ?? Source);
        var compilation = new SemanticModelCompiler().Compile("HandlerModel", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var commands = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Commands).ToArray();
        var requirements = compilation.ImplementationRequirements.Select(requirement =>
        {
            var command = commands.Single(command => command.Handler?.RequirementId == requirement.RequirementId);
            var content = bodies is null ? body : bodies[command.Name];
            return requirement with
            {
                AttachmentResolution = SemanticAttachmentResolution.Resolved,
                Language = "csharp",
                ContentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content))),
                IsProvisional = false
            };
        }).ToImmutableArray();
        var descriptors = compilation.TypedContextDescriptors;
        return new(model, SemanticExecutionPlan.Compile(model, []).Plan!, CratisRendering.CreateProfile("HandlerModel", new("HandlerApp", "Orders")), new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = requirements,
            ImplementationContents = requirements.ToImmutableDictionary(requirement => requirement.RequirementId, requirement => bodies is null ? body : bodies[commands.Single(command => command.Handler?.RequirementId == requirement.RequirementId).Name]),
            TypedContextDescriptors = descriptors
        };
    }

    internal static ArtifactRenderPlan Plan(ArtifactRenderRequest request) => new CratisArtifactRenderPlanner().Plan(request);
}
