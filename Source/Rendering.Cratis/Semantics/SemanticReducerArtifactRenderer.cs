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
        var builder = new CSharpCodeBuilder()
            .Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path))
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Reducers")
            .Using($"{context.RootNamespace}.TypedContexts")
            .Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(model.Id).Path))
            .OpenBlock($"public class {name} : IReducerFor<{modelType}>");
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

            builder.ExpressionMember(
                $"public {modelType}? On({eventType} @event, {modelType}? current, EventContext eventContext)",
                $"Transition_{suffix}(new {wrapper}(current, @event, eventContext.EventSourceId.Value, ReducerContextValues.Tenant(eventContext.Namespace), eventContext.Occurred, checked((long)eventContext.SequenceNumber.Value)))")
                .BlankLine()
                .OpenBlock($"static {modelType}? Transition_{suffix}({wrapper} context)")
                .Raw(body!)
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
