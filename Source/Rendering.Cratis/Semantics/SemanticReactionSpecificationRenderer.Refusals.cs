// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticReactionSpecificationRenderer
{
    static RenderedFile RenderRefusals(SemanticSpecification specification, SemanticApplicationContext context, SemanticReactionSpecificationPrelude prelude)
    {
        var located = context.DeclaringSlice(specification.Id);
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}";
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var invoked = prelude.Selection.Trigger.Invokes.Select(invocation => context.Commands[invocation.Command]).ToArray();

        // One invoked command runs in a scenario typed by it: Arc derives the decision mode from that command's assembly. Several run in an untyped scenario that enables decision reads up front, before any seeding or execution.
        var typed = invoked.Length == 1 ? CommandType(invoked[0], types) : null;
        var decision = invoked.Length == 1 && !invoked[0].Reads.IsDefaultOrEmpty;
        var builder = context.SpecificationBuilder().Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Arc.Commands").Using("Cratis.Arc.Chronicle.Testing.Commands").Using("Cratis.Arc.Testing.Commands")
            .Using("Cratis.Specifications").Using("Microsoft.Extensions.DependencyInjection").Using("NSubstitute").Using("Xunit")
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line(typed is null
                ? "readonly global::Cratis.Arc.Testing.Commands.CommandScenario<object> _scenario = new global::Cratis.Arc.Testing.Commands.CommandScenario<object>().UseDecisionReads();"
                : $"readonly global::Cratis.Arc.Testing.Commands.CommandScenario<{typed}> _scenario = new();")
            .Line(SemanticSpecificationValuesRenderer.Declarations)
            .Line("global::Cratis.Chronicle.Events.AppendedEvent[] _facts = [];")
            .Line("bool _settled;")
            .BlankLine().OpenBlock("async global::System.Threading.Tasks.Task Because()");
        if (prelude.Selection.Trigger.Invokes.Any(invocation => context.Commands[invocation.Command].Authorization is not null))
        {
            builder.Line($"global::{context.RootNamespace}.GeneratedPolicies.Registration.Register(_scenario.Services);");
        }
        foreach (var (command, pins) in invoked.Select(command => (command, pins: specification.GivenGenerated.Where(pinned => pinned.Command == command.Id).GroupBy(pinned => pinned.Property).Select(group => group.First()).ToArray())))
        {
            foreach (var pin in pins)
            {
                var property = command.Properties.Single(candidate => candidate.Id == pin.Property);
                builder.Line($"_scenario.PinGeneratedValue({CSharpCodeBuilder.StringLiteral(property.Name)}, {types.Value(pin.Value, property.Type)});");
            }
        }

        builder.Line("var accessor = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor());")
            .Line("_scenario.Services.AddSingleton<global::Cratis.Arc.Authorization.ICurrentPrincipalAccessor>(accessor);")
            .Line("_scenario.Services.AddSingleton<global::Cratis.Arc.Authorization.ICurrentPrincipalOverride>(accessor);");
        foreach (var given in specification.GivenEvents)
        {
            var contract = context.Events[given.EventContract];
            var arguments = contract.Properties.Select(property => types.Value(given.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
            var source = SemanticSpecificationEventSources.Expression(context, types, given);
            builder.Line(decision
                ? $"_scenario.Given.ForEventSource({source}).Events(new {types.EventType(contract)}({string.Join(", ", arguments)}));"
                : $"(await _scenario.EventLog.Append({source}, new {types.EventType(contract)}({string.Join(", ", arguments)}))).IsSuccess.ShouldBeTrue();");
        }
        var append = prelude.Append;
        var appended = context.Events[append.EventContract];
        var appendArguments = appended.Properties.Select(property => types.Value(append.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
        var appendSource = types.EventSourceExpression(types.Value(append.EventSource!.Value, append.EventSource.Type), append.EventSource.Type);
        var reactor = ReactorType(prelude, context);
        builder.Line($"var reactor = new {reactor}();")
            .Line($"var @event = new {types.EventType(appended)}({string.Join(", ", appendArguments)});")
            .Line($"(await _scenario.EventLog.Append({appendSource}, @event)).IsSuccess.ShouldBeTrue();")
            .Line("var before = await _scenario.EventLog.GetFromSequenceNumber(global::Cratis.Chronicle.Events.EventSequenceNumber.First);")
            .Line("var initial = before.Count;")
            .Line("var eventContext = before.Last().Context;")
            .Line($"var effects = reactor.{Identifiers.ToPascalCase(appended.Name)}(@event, eventContext);")
            .Line("// Only the scope adapter is substituted. Each command uses the real generated Arc pipeline and validators.")
            .Line("var pipeline = Substitute.For<global::Cratis.Arc.Commands.ICommandPipeline>();")
            .Line($"pipeline.Execute(Arg.Any<object>(), Arg.Any<global::System.IServiceProvider>(), Arg.Any<global::Cratis.Arc.Validation.ValidationResultSeverity?>()).Returns(call => _scenario.Execute({(typed is null ? "call.ArgAt<object>(0)" : $"({typed})call.ArgAt<object>(0)")}));")
            .Line("using var provider = new global::Microsoft.Extensions.DependencyInjection.ServiceCollection().AddSingleton(pipeline).BuildServiceProvider();")
            .Line("global::Cratis.Arc.Chronicle.Reactors.ICommandSideEffectExecutor executor = new global::Cratis.Arc.Chronicle.Reactors.CommandSideEffectExecutor(provider.GetRequiredService<global::Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(), new global::Cratis.Arc.Authorization.SystemExecution(accessor));")
            .Line("var reactorContext = new global::Cratis.Chronicle.Reactors.SideEffects.ReactorContext(eventContext, reactor, new());")
            .Line("var eventStore = (global::Cratis.Chronicle.IEventStore)_scenario.Services.Last(registration => registration.ServiceType == typeof(global::Cratis.Chronicle.IEventStore)).ImplementationInstance!;")
            .Line("var result = await executor.Execute(effects, reactorContext, eventStore);")
            .Line("_settled = result.IsSuccess;")
            .Line("var persisted = await _scenario.EventLog.GetFromSequenceNumber(global::Cratis.Chronicle.Events.EventSequenceNumber.First);")
            .Line("_facts = [.. persisted.Skip(initial)];")
            .EndBlock().BlankLine();
        RenderRefusalOutcome(builder, specification, context, types);
        builder.BlankLine().ExpressionMember("public void Dispose()", "_scenario.Dispose()").EndBlock();

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]), types.SpecificationContent(builder));
    }

    static void RenderRefusalOutcome(CSharpCodeBuilder builder, SemanticSpecification specification, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        if (specification.ThenDenied)
        {
            builder.Line("[global::Xunit.FactAttribute] void should_not_settle() => _settled.ShouldBeFalse();")
                .Line("[global::Xunit.FactAttribute] void should_be_denied() => _scenario.LastResult!.IsAuthorized.ShouldBeFalse();");
            return;
        }
        if (!specification.ThenErrors.IsEmpty)
        {
            builder.Line("[global::Xunit.FactAttribute] void should_not_settle() => _settled.ShouldBeFalse();")
                .Line("[global::Xunit.FactAttribute] void should_not_succeed() => _scenario.LastResult!.ShouldNotBeSuccessful();");
            var error = specification.ThenErrors[0];
            if (error.Message is { } message)
            {
                var member = message.StartsWith("$strings.", StringComparison.Ordinal) ? "State" : "Message";
                builder.Line($"[global::Xunit.FactAttribute] void should_report_the_expected_error() => _scenario.LastResult!.ValidationResults.First().{member}.ShouldEqual({CSharpCodeBuilder.StringLiteral(message)});");
            }
            return;
        }
        builder.Line("[global::Xunit.FactAttribute] void should_settle() => _settled.ShouldBeTrue();")
            .Line($"[global::Xunit.FactAttribute] void should_append_exactly_the_expected_events() => _facts.Length.ShouldEqual({specification.ThenEvents.Length});");
        foreach (var (expected, index) in specification.ThenEvents.Select((value, index) => (value, index)))
        {
            var contract = context.Events[expected.EventContract];
            var values = ProductionValues(types, context, index);
            var fields = contract.Properties.Select(property => (property, expected.Values.Single(value => value.TargetProperty == property.Id).Value)).ToArray();
            RenderExpectedCollections(builder, fields, values);
            var predicates = fields.Select(field => ValuePredicate(field.property, field.Value, values));
            var source = expected.EventSource is { } identity
                ? $" && _facts[{index}].Context.EventSourceId == {types.EventSourceExpression(types.Value(identity.Value, identity.Type), identity.Type)}" : string.Empty;
            builder.Line($"[global::Xunit.FactAttribute] void should_append_event_{index + 1}() => (_facts[{index}].Content is {types.EventType(contract)} @event{string.Concat(predicates.Select(predicate => $" && {predicate}"))}{source}).ShouldBeTrue();");
        }
    }

    static string? CommandType(SemanticCommand command, SemanticTypeSystem types) =>
        types.SliceType(command.Id, command.Name);
}
