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
        var model = State(context, reducer);
        var modelType = $"global::{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(model.Id).Path)}.{Identifiers.ToPascalCase(model.Name)}";
        var name = Identifiers.ToPascalCase(reducer.Name);
        var builder = context.CodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        (reducer.Target == SemanticProjectionTargetKind.Event
            ? SemanticReducerBindingEnvironment.Create(located, context, reducer)
            : context.ReducerBindings(located, reducer)).Configure(builder);
        if (reducer.Target == SemanticProjectionTargetKind.Event) builder.Using("System.Linq");
        if (reducer.From is { } filter)
        {
            SemanticObserverFilterRendering.Attribute(builder, filter, context);
        }
        builder.OpenBlock($"public class {name} : global::Cratis.Chronicle.Reducers.IReducerFor<{modelType}>");
        RenderTransitions(builder, reducer, context);
        RenderPublicationMembers(builder, reducer);
        builder.EndBlock();
        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), builder.ToString())
        {
            Sources = [reducer.ReadModel, .. reducer.Transitions.Select(_ => _.EventContract), located.Slice.Id]
        };
    }

    internal static void RenderTransitions(CSharpCodeBuilder builder, SemanticReducer reducer, SemanticApplicationContext context, string? analysisRequirement = null, string? analysisBody = null)
    {
        var model = State(context, reducer);
        var modelType = $"global::{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(model.Id).Path)}.{Identifiers.ToPascalCase(model.Name)}";
        var name = Identifiers.ToPascalCase(reducer.Name);
        var self = SelfReduces(context, reducer);
        var check = ReadModelIdentityCheck(context, reducer);

        foreach (var transition in reducer.Transitions)
        {
            var @event = context.Events[transition.EventContract];
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
            var eventType = $"global::{eventNamespace}.{Identifiers.ToPascalCase(@event.Name)}";
            var descriptor = context.Request.TypedContextDescriptors.Single(_ => _.RequirementId == transition.RequirementId && _.OperationId == reducer.ReadModel);
            var wrapper = context.ContextNames.ContextWrapper(descriptor);
            var helper = context.InlineNames.ReducerTransition(transition.RequirementId);
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
                    .Line($"var result = {helper}{(self ? $".{SelfTransitionMethod}" : string.Empty)}(new global::{context.TypedContextsNamespace}.{wrapper}(current, @event, originalSourceId, {tenant}, eventContext.Occurred, checked((long)eventContext.SequenceNumber.Value)));");
                RenderResult(builder, reducer, check);
            }
            else
            {
                builder.Line("throw new global::System.NotImplementedException();");
            }
            builder.EndBlock().BlankLine();
            if (self) builder.OpenBlock($"static class {helper}");
            builder.OpenBlock($"{(self ? "internal static" : "static")} {modelType}? {(self ? SelfTransitionMethod : helper)}(global::{context.TypedContextsNamespace}.{wrapper} context)")
                .RawVerbatim(body!, transition.RequirementId)
                .EndBlock()
                .BlankLine();
            if (self) builder.EndBlock().BlankLine();
        }
    }

    /// <summary>
    /// Resolves the state a reducer builds: the event contract of an event-target reducer, otherwise its read model.
    /// </summary>
    /// <param name="context">The application context.</param>
    /// <param name="reducer">The reducer.</param>
    /// <returns>The identifier, name and properties of the reduced state.</returns>
    internal static (SemanticId Id, string Name, System.Collections.Immutable.ImmutableArray<SemanticProperty> Properties) State(SemanticApplicationContext context, SemanticReducer reducer) =>
        reducer.Target == SemanticProjectionTargetKind.Event
            ? (context.Events[reducer.ReadModel].Id, context.Events[reducer.ReadModel].Name, context.Events[reducer.ReadModel].Properties)
            : (context.ReadModels[reducer.ReadModel].Id, context.ReadModels[reducer.ReadModel].Name, context.ReadModels[reducer.ReadModel].Properties);

    static void RenderPublicationMembers(CSharpCodeBuilder builder, SemanticReducer reducer)
    {
        if (reducer.Target != SemanticProjectionTargetKind.Event) return;
        SemanticPublicationArtifactRenderer.RenderComparison(builder);
        builder.Line("sealed class PublicEventCannotBeRetracted() : global::System.Exception(\"A published public event cannot be retracted.\");");
    }

    static void RenderResult(CSharpCodeBuilder builder, SemanticReducer reducer, string? check)
    {
        if (reducer.Target == SemanticProjectionTargetKind.Event)
        {
            builder.Line("if (result is null) throw new PublicEventCannotBeRetracted();")
                .Line("return current is not null && SamePublicValue(current, result) ? current : result;");
            return;
        }
        builder.OpenBlock($"if (result is not null && ({check}))")
            .Line("throw new global::System.InvalidOperationException(\"Reducer returned a read model with an identifier different from the event source.\");")
            .EndBlock()
            .Line("return result;");
    }

    static bool SelfReduces(SemanticApplicationContext context, SemanticReducer reducer) =>
        reducer.Target != SemanticProjectionTargetKind.Event && context.SelfReduces(reducer.ReadModel);

    static string? ReadModelIdentityCheck(SemanticApplicationContext context, SemanticReducer reducer)
    {
        if (reducer.Target == SemanticProjectionTargetKind.Event) return null;
        var identifier = context.ReadModels[reducer.ReadModel].Properties.Single(property => property.IsIdentifier);
        return IdentityCheck(identifier.Type, Identifiers.ToPascalCase(identifier.Name), context);
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
