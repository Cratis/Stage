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
    internal const string SelfTransitionMethod = "Apply";

    internal static RenderedFile Render(LocatedSemanticSlice located, SemanticReducer reducer, SemanticApplicationContext context)
    {
        var model = context.ReadModels[reducer.ReadModel];
        var modelType = $"global::{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(model.Id).Path)}.{Identifiers.ToPascalCase(model.Name)}";
        var name = Identifiers.ToPascalCase(reducer.Name);
        var builder = context.CodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        context.ReducerBindings(located, reducer).Configure(builder);
        if (reducer.From is { } filter)
        {
            SemanticObserverFilterRendering.Attribute(builder, filter, context);
        }
        builder.OpenBlock($"public class {name} : global::Cratis.Chronicle.Reducers.IReducerFor<{modelType}>");
        RenderTransitions(builder, reducer, context);
        builder.EndBlock();
        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), builder.ToString())
        {
            Sources = [reducer.ReadModel, .. reducer.Transitions.Select(_ => _.EventContract), located.Slice.Id]
        };
    }

    internal static string HelperName(string requirementId) =>
        "Transition_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requirementId)))[..8];

    internal static void RenderTransitions(CSharpCodeBuilder builder, SemanticReducer reducer, SemanticApplicationContext context, string? analysisRequirement = null, string? analysisBody = null)
    {
        var model = context.ReadModels[reducer.ReadModel];
        var modelType = $"global::{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(model.Id).Path)}.{Identifiers.ToPascalCase(model.Name)}";
        var name = Identifiers.ToPascalCase(reducer.Name);
        var keyType = model.Properties.Single(_ => _.IsIdentifier).Type;
        var self = context.SelfReduces(reducer.ReadModel);
        var keyMember = Identifiers.ToPascalCase(model.Properties.Single(_ => _.IsIdentifier).Name);
        var check = IdentityCheck(keyType, keyMember, context);

        foreach (var transition in reducer.Transitions)
        {
            var @event = context.Events[transition.EventContract];
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
            var eventType = $"global::{eventNamespace}.{Identifiers.ToPascalCase(@event.Name)}";
            var descriptor = context.Request.TypedContextDescriptors.Single(_ => _.RequirementId == transition.RequirementId && _.OperationId == reducer.ReadModel);
            var wrapper = $"TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}";
            var helper = HelperName(transition.RequirementId);
            var diagnostics = System.Collections.Immutable.ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
            var body = analysisRequirement == transition.RequirementId ? analysisBody : "throw new global::System.NotImplementedException();";
            if (analysisRequirement is null && !SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, transition.RequirementId, reducer.ReadModel, diagnostics, out body))
            {
                throw new InvalidTypedContext($"Reducer '{name}' lost its verified body after admission.");
            }

            if (!context.ReducerContextReads.TryGetValue(transition.RequirementId, out var contextReads) && analysisRequirement is null)
            {
                throw new InvalidTypedContext($"Reducer '{name}' lost its analyzed context reads after admission.");
            }

            var tenant = contextReads?.Contains("Tenant") == true
                ? $"global::{context.RootNamespace}.TypedContexts.ReducerContextValues.Tenant(eventContext.Namespace)"
                : $"global::{context.RootNamespace}.TypedContexts.TenantId.NotSet";
            builder.OpenBlock($"public {(self ? "static " : string.Empty)}{modelType}? {(self ? "Reduce" : "On")}({eventType} @event, {modelType}? current, global::Cratis.Chronicle.Events.EventContext eventContext)");
            if (analysisRequirement is null)
            {
                builder.Line("var originalSourceId = eventContext.EventSourceId.Value;")
                .Line($"var result = {helper}{(self ? $".{SelfTransitionMethod}" : string.Empty)}(new global::{context.TypedContextsNamespace}.{wrapper}(current, @event, originalSourceId, {tenant}, eventContext.Occurred, checked((long)eventContext.SequenceNumber.Value)));")
                .OpenBlock($"if (result is not null && ({check}))")
                .Line("throw new global::System.InvalidOperationException(\"Reducer returned a read model with an identifier different from the event source.\");")
                .EndBlock()
                .Line("return result;");
            }
            else
            {
                builder.Line("throw new global::System.NotImplementedException();");
            }
            builder.EndBlock().BlankLine();
            if (self) builder.OpenBlock($"static class {helper}");
            builder.OpenBlock($"{(self ? "internal static" : "static")} {modelType}? {(self ? SelfTransitionMethod : helper)}(global::{context.TypedContextsNamespace}.{wrapper} context)")
                .RawVerbatim(body!)
                .EndBlock()
                .BlankLine();
            if (self) builder.EndBlock().BlankLine();
        }
    }

    static string IdentityCheck(SemanticTypeReference keyType, string keyMember, SemanticApplicationContext context)
    {
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
        return check;
    }
}
