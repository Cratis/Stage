// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders one append through a reactor, optionally executing its returned command under its declared identity.
/// </summary>
internal static partial class SemanticReactionSpecificationRenderer
{
    internal static RenderedFile Render(SemanticSpecification specification, SemanticApplicationContext context)
    {
        var selected = SemanticSpecificationAdmission.SelectReaction(context, specification);
        var prelude = new SemanticReactionSpecificationPrelude(selected, specification.WhenAppended!);
        if (!selected.Trigger.Invokes.IsEmpty)
        {
            return SemanticCommandSpecificationRenderer.Render(selected.Lower(specification, context), context, prelude);
        }

        return RenderProductions(specification, context, prelude);
    }

    internal static void RenderHeader(CSharpCodeBuilder builder, SemanticReactionSpecificationPrelude prelude, SemanticApplicationContext context) =>
        builder.Using("Cratis.Chronicle.Testing.Reactors")
            .Line($"readonly global::Cratis.Chronicle.Testing.Reactors.ReactorScenario<{ReactorType(prelude, context)}> _reactor = new();");

    internal static void RenderBecause(SpecificationScope scope)
    {
        var builder = scope.Builder;
        var prelude = scope.ReactionPrelude!;
        builder.Using("System.Reflection")
            .Using("Cratis.Arc.Authorization")
            .Using("Cratis.Arc.Chronicle.Reactors")
            .Using("Cratis.Arc.Http")
            .OpenBlock("async global::System.Threading.Tasks.Task Because()");
        RenderAppend(builder, prelude, scope.Context, scope.Types);
        builder.Line($"var command = _reactor.Produced.OfType<{scope.CommandName}>().Single();")
            .Line("// Mirrors Arc's CommandSideEffectExecutor: identity comes from the reactor's own attribute.")
            .Line($"var identity = typeof({ReactorType(prelude, scope.Context)}).GetCustomAttribute<global::Cratis.Arc.Chronicle.Reactors.ExecuteCommandsAsSystemAttribute>();")
            .Line("var accessor = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor());")
            .Line("using var system = identity is null ? null : new global::Cratis.Arc.Authorization.SystemExecution(accessor).AsSystem([.. identity.Roles]);")
            .Line("_result = await _scenario.Execute(command);")
            .EndBlock().BlankLine();
        var predicates = scope.Command.Properties.Where(property => !property.IsGenerated).Select(property =>
            ValuePredicate(property, scope.Specification.When!.Values.Single(value => value.TargetProperty == property.Id).Value, scope.Types, "command"));
        builder.Line($"[global::Xunit.FactAttribute] void should_invoke_the_expected_command() => (_reactor.Produced.Count == 1 && _reactor.Produced.Single() is {scope.CommandName} command{string.Concat(predicates.Select(predicate => $" && {predicate}"))}).ShouldBeTrue();")
            .BlankLine();
    }

    internal static void RenderDispose(CSharpCodeBuilder builder) => builder.OpenBlock("public void Dispose()")
        .Line("_reactor.Dispose();").Line("_scenario.Dispose();").EndBlock();

    static string ReactorType(SemanticReactionSpecificationPrelude prelude, SemanticApplicationContext context)
    {
        var declaring = SemanticVersionFeatures.Slices(context.Application).Single(slice =>
            !slice.Reactions.IsDefault && slice.Reactions.Any(reaction => reaction.Id == prelude.Selection.Reaction.Id));

        return $"global::{SliceNaming.Namespace(context.RootNamespace, context.Slice(declaring.Id).Path)}.{Identifiers.ToPascalCase(prelude.Selection.Reaction.Name)}";
    }

    static void RenderAppend(CSharpCodeBuilder builder, SemanticReactionSpecificationPrelude prelude, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        var append = prelude.Append;
        var @event = context.Events[append.EventContract];
        var arguments = @event.Properties.Select(property => types.Value(append.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
        var source = append.EventSource is { } identity
            ? types.EventSourceExpression(types.Value(identity.Value, identity.Type), identity.Type) : "\"when-append\"";
        if (append.EventSource is null)
        {
            builder.Line("// The model has no append source; this reaction never uses the trigger destination.");
        }
        builder.Line($"await _reactor.Given.ForEventSource({source}){SemanticSpecificationRouteRendering.Seed(append.Route, context)}.Events(new {types.EventType(@event)}({string.Join(", ", arguments)}));");
    }

    static RenderedFile RenderProductions(SemanticSpecification specification, SemanticApplicationContext context, SemanticReactionSpecificationPrelude prelude)
    {
        var located = context.DeclaringSlice(specification.Id);
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}";
        var types = new SemanticTypeSystem(context);
        var builder = new CSharpCodeBuilder().Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Specifications").Using("Xunit");
        context.Docs(specification.Id).Render(builder)
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable");
        RenderHeader(builder, prelude, context);
        builder.BlankLine().OpenBlock("async global::System.Threading.Tasks.Task Because()");
        RenderAppend(builder, prelude, context, types);
        builder.EndBlock().BlankLine();
        RenderProductionExpectations(builder, specification, prelude, context, types);
        builder.BlankLine().ExpressionMember("public void Dispose()", "_reactor.Dispose()").EndBlock();

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]), $"#if DEBUG\n{builder}\n#endif\n");
    }

    static string ValuePredicate(SemanticProperty property, SemanticValue value, SemanticTypeSystem types, string variable)
    {
        var member = $"{variable}.{Identifiers.ToPascalCase(property.Name)}";
        if (value is SemanticNullValue) return $"{member} is null";
        var expected = types.Value(value, property.Type);

        return property.Type.IsCollection ? $"{member}.SequenceEqual({expected})" : $"{member} == {expected}";
    }
}

/// <summary>
/// The append fixture and selected reactor that precede a command scenario.
/// </summary>
/// <param name="Selection">The statically selected reaction.</param>
/// <param name="Append">The modeled append fixture.</param>
internal sealed record SemanticReactionSpecificationPrelude(SemanticReactionSpecification Selection, SemanticSpecificationAppend Append);
