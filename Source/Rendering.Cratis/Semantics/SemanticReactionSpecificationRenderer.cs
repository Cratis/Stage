// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders one append or located redelivery through a reactor, optionally executing its returned command under its declared identity.
/// </summary>
internal static partial class SemanticReactionSpecificationRenderer
{
    internal static RenderedFile Render(SemanticSpecification specification, SemanticApplicationContext context)
    {
        var selected = SemanticSpecificationAdmission.SelectReaction(context, specification);
        var prelude = new SemanticReactionSpecificationPrelude(selected, SemanticReactionSpecification.Input(specification), specification.WhenRedelivered is null ? null : specification);
        if (selected.Trigger.Invokes.Any(invocation => !invocation.OnRefused.IsEmpty))
        {
            return RenderRefusals(specification, context, prelude);
        }
        if (!selected.Trigger.Invokes.IsEmpty)
        {
            // Screenplay decision 0115: a command another slice owns is decided, so its refusal still fails, but its facts are not asserted.
            var decidedOnly = SemanticSpecificationAdmission.InvokesForeignCommand(context, specification, selected);
            return SemanticCommandSpecificationRenderer.Render(selected.Lower(specification, context), context, prelude with { DecidedOnly = decidedOnly });
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
        var values = new ValueScope(scope.Types, scope.Context.MemberNames, "command", "_expected_command");
        var fields = scope.Command.Properties.Where(property => !property.IsGenerated).Select(property =>
            (property, scope.Specification.When!.Values.Single(value => value.TargetProperty == property.Id).Value)).ToArray();
        RenderExpectedCollections(builder, fields, values);
        builder.Line("[global::Xunit.FactAttribute] void should_invoke_exactly_one_command() => _reactor.Produced.Count.ShouldEqual(1);")
            .Line($"[global::Xunit.FactAttribute] void should_invoke_the_expected_command() => (_reactor.Produced.Single() is {scope.CommandName}).ShouldBeTrue();");
        foreach (var (property, value) in fields)
        {
            builder.Line($"[global::Xunit.FactAttribute] void should_invoke_with_{Identifiers.ToSnakeCase(property.Name)}() => (_reactor.Produced.Single() is {scope.CommandName} command && {ValuePredicate(property, value, values)}).ShouldBeTrue();");
        }
        builder.BlankLine();
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
        if (prelude.Redelivery is { } specification)
        {
            RenderRedelivery(builder, specification, context, types);
            return;
        }
        var append = prelude.Append;
        var @event = context.Events[append.EventContract];
        var arguments = @event.Properties.Select(property => types.Value(append.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
        var source = append.EventSource is { } identity
            ? types.EventSourceExpression(types.Value(identity.Value, identity.Type), identity.Type) : CSharpCodeBuilder.StringLiteral(SemanticSpecificationEventSources.Placeholder);
        if (append.EventSource is null)
        {
            builder.Line("// The model has no append source; this reaction never uses the trigger destination.");
        }
        builder.Line("// Given facts establish history; reactions only observe the append.")
            .Line($"await _reactor.Given.ForEventSource({source}){SemanticSpecificationRouteRendering.Seed(append.Route, context, types)}.Events(new {types.EventType(@event)}({string.Join(", ", arguments)}));");
    }

    static void RenderRedelivery(CSharpCodeBuilder builder, SemanticSpecification specification, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        builder.Line("// History allocates original contexts without invoking any reactor or appending the inputs again.");
        foreach (var (given, index) in specification.GivenEvents.Select((given, index) => (given, index)))
        {
            var contract = context.Events[given.EventContract];
            var arguments = contract.Properties.Select(property => types.Value(given.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
            var source = SemanticSpecificationEventSources.Expression(context, types, given);
            builder.Line($"var occurrence{index} = await _reactor.Given.ForEventSource({source}){SemanticSpecificationRouteRendering.Seed(given.Route, context, types)}.History(new {types.EventType(contract)}({string.Join(", ", arguments)}));");
        }
        builder.Line($"await _reactor.Redeliver(occurrence{specification.WhenRedelivered!.Given});");
    }

    static RenderedFile RenderProductions(SemanticSpecification specification, SemanticApplicationContext context, SemanticReactionSpecificationPrelude prelude)
    {
        var located = context.DeclaringSlice(specification.Id);
        var behavior = SemanticSpecificationClassNames.Behavior(specification);
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var builder = context.SpecificationBuilder().Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Specifications").Using("Xunit");
        context.Docs(specification.Id).Render(builder)
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable");
        RenderHeader(builder, prelude, context);
        builder.Line(SemanticSpecificationValuesRenderer.Declarations).BlankLine().OpenBlock("async global::System.Threading.Tasks.Task Because()");
        RenderAppend(builder, prelude, context, types);
        builder.EndBlock().BlankLine();
        RenderProductionExpectations(builder, specification, prelude, context, types);
        builder.BlankLine().ExpressionMember("public void Dispose()", "_reactor.Dispose()").EndBlock();

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]), types.SpecificationContent(builder));
    }

    static string ValuePredicate(SemanticProperty property, SemanticValue value, ValueScope scope)
    {
        var name = scope.Names.Property(property);
        var member = $"{scope.Variable}.{name}";
        if (value is SemanticNullValue) return $"{member} is null";

        return property.Type.IsCollection
            ? $"({member} is {{ }} actual{name} && actual{name}.SequenceEqual({scope.CollectionName(property)}))"
            : $"{member} == {scope.Types.Value(value, property.Type)}";
    }

    static void RenderExpectedCollections(CSharpCodeBuilder builder, IEnumerable<(SemanticProperty Property, SemanticValue Value)> fields, ValueScope scope)
    {
        foreach (var (property, value) in fields.Where(field => field.Property.Type.IsCollection && field.Value is SemanticArrayValue))
        {
            var elementType = property.Type with { IsCollection = false, IsOptional = false };
            builder.Line($"static readonly {scope.Types.Type(elementType)}[] {scope.CollectionName(property)} = {scope.Types.Value(value, property.Type)};");
        }
    }

    sealed record ValueScope(SemanticTypeSystem Types, GeneratedMemberNames Names, string Variable, string Prefix)
    {
        internal string CollectionName(SemanticProperty property) => $"{Prefix}_{Identifiers.ToSnakeCase(property.Name)}";
    }
}

/// <summary>
/// The append fixture and selected reactor that precede a command scenario.
/// </summary>
/// <param name="Selection">The statically selected reaction.</param>
/// <param name="Append">The modeled input fixture.</param>
/// <param name="Redelivery">The original specification when delivering recorded history.</param>
internal sealed record SemanticReactionSpecificationPrelude(SemanticReactionSpecification Selection, SemanticSpecificationAppend Append, SemanticSpecification? Redelivery = null)
{
    /// <summary>
    /// Gets a value indicating whether the invoked command belongs to another slice, so it is decided but its facts are not asserted.
    /// </summary>
    public bool DecidedOnly { get; init; }
}
