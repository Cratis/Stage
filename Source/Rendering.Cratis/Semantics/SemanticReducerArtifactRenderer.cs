// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Renders one Chronicle reducer with one statically dispatched overload per observed event.</summary>
internal static class SemanticReducerArtifactRenderer
{
    internal static RenderedFile Render(LocatedSemanticSlice located, SemanticReducer reducer, SemanticApplicationContext context)
    {
        var model = context.ReadModels[reducer.ReadModel];
        var modelType = $"global::{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(model.Id).Path)}.{Identifiers.ToPascalCase(model.Name)}";
        var name = Identifiers.ToPascalCase(reducer.Name);
        var keyType = model.Properties.Single(_ => _.IsIdentifier).Type;
        var keyMember = Identifiers.ToPascalCase(model.Properties.Single(_ => _.IsIdentifier).Name);
        var concept = keyType.Kind == SemanticTypeReferenceKind.Concept;
        var uuid = concept ? context.Concepts[keyType.Target].Primitive == SemanticPrimitiveType.Uuid :
            keyType.Primitive == SemanticPrimitiveType.Uuid;
        var key = $"result.{keyMember}";
        var typedValue = concept ? $"{key}.TypedValue" : key;
        var value = concept ? $"{key}.Value" : key;
        var typedWire = uuid ? $"{typedValue}.ToString()" : typedValue;
        var valueWire = uuid ? $"{value}.ToString()" : value;
        var check = $"!global::System.String.Equals({typedWire}, originalSourceId, global::System.StringComparison.Ordinal)";
        if (concept)
        {
            check = $"{key} is null || {check} || !global::System.String.Equals({valueWire}, originalSourceId, global::System.StringComparison.Ordinal)";
        }

        var builder = new CSharpCodeBuilder()
            .Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path))
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Reducers")
            .Using($"{context.RootNamespace}.TypedContexts")
            .Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(model.Id).Path))
            .OpenBlock($"public class {name} : global::Cratis.Chronicle.Reducers.IReducerFor<{modelType}>");
        foreach (var transition in reducer.Transitions)
        {
            var @event = context.Events[transition.EventContract];
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
            var eventType = $"global::{eventNamespace}.{Identifiers.ToPascalCase(@event.Name)}";
            builder.Using(eventNamespace);
            var descriptor = context.Request.TypedContextDescriptors.Single(_ => _.RequirementId == transition.RequirementId && _.OperationId == reducer.ReadModel);
            var wrapper = $"TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}";
            var suffix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(transition.RequirementId)))[..8];
            var diagnostics = System.Collections.Immutable.ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
            if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, transition.RequirementId, reducer.ReadModel, diagnostics, out var body))
            {
                throw new InvalidTypedContext($"Reducer '{name}' lost its verified body after admission.");
            }

            var tenant = context.ReducerContextReads[transition.RequirementId].Contains("Tenant")
                ? $"global::{context.RootNamespace}.TypedContexts.ReducerContextValues.Tenant(eventContext.Namespace)"
                : $"global::{context.RootNamespace}.TypedContexts.TenantId.NotSet";
            builder.OpenBlock($"public {modelType}? On({eventType} @event, {modelType}? current, global::Cratis.Chronicle.Events.EventContext eventContext)")
                .Line("var originalSourceId = eventContext.EventSourceId.Value;")
                .Line($"var result = Transition_{suffix}(new global::{context.RootNamespace}.TypedContexts.{wrapper}(current, @event, originalSourceId, {tenant}, eventContext.Occurred, checked((long)eventContext.SequenceNumber.Value)));")
                .OpenBlock($"if (result is not null && ({check}))")
                .Line("throw new global::System.InvalidOperationException(\"Reducer returned a read model with an identifier different from the event source.\");")
                .EndBlock()
                .Line("return result;")
                .EndBlock()
                .BlankLine()
                .OpenBlock($"static {modelType}? Transition_{suffix}(global::{context.RootNamespace}.TypedContexts.{wrapper} context)")
                .RawVerbatim(body!)
                .EndBlock()
                .BlankLine();
        }

        builder.EndBlock();
        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), builder.ToString())
        {
            Sources = [reducer.ReadModel, .. reducer.Transitions.Select(_ => _.EventContract)]
        };
    }
}
