// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles;

/// <summary>
/// Renders the state machines of a model as Cratis.Orleans event-sourced state machines and its workflows as workflow
/// definitions with the performers, signals and triggers that run them on the silo the application hosts.
/// </summary>
internal static class SemanticLifecycleArtifactRenderer
{
    const string StateMachines = "global::Cratis.Orleans.Chronicle.StateMachines";
    const string Workflows = "global::Cratis.Orleans.Workflows";

    internal static string Folder(SemanticApplicationContext context) => string.Join('/', context.Domain.Append("Lifecycles"));

    internal static string Namespace(SemanticApplicationContext context) => string.Join('.', new[] { context.RootNamespace }.Concat(context.Domain).Append("Lifecycles"));

    internal static bool HasEventTriggers(SemanticApplicationContext context) => context.Application.Workflows.Any(workflow => EventTriggers(workflow).Any());

    internal static IEnumerable<RenderedFile> Render(SemanticApplicationContext context)
    {
        var application = context.Application;
        foreach (var machine in application.StateMachines)
        {
            yield return RenderStateMachine(context, machine);
        }

        if (application.Workflows.IsDefaultOrEmpty) yield break;
        foreach (var workflow in application.Workflows)
        {
            yield return RenderDefinition(context, WorkflowGraphSpecs.For(context.Request.ExecutionPlan, workflow));
        }

        if (RenderActions(context) is { } actions) yield return actions;
        if (RenderSignals(context) is { } signals) yield return signals;
        if (RenderTriggers(context) is { } triggers) yield return triggers;
    }

    static IEnumerable<SemanticWorkflowNode> EventTriggers(SemanticWorkflow workflow) =>
        workflow.Nodes.Where(node => node is { Kind: SemanticWorkflowNodeKind.Trigger, TriggerKind: SemanticWorkflowTriggerKind.Event });

    static RenderedFile RenderStateMachine(SemanticApplicationContext context, SemanticStateMachine machine)
    {
        var types = new SemanticTypeSystem(context);
        var model = context.ReadModels[machine.ReadModel];
        var modelType = types.SliceType(model.Id, model.Name);
        var name = Identifiers.ToPascalCase(machine.Name);
        var self = $"global::{Namespace(context)}.{name}";
        var initial = Identifiers.ToPascalCase(machine.States.Single(state => state.IsInitial).Name);
        var builder = context.CodeBuilder().Namespace(Namespace(context));
        builder.Summary($"The grain of the {machine.Name} state machine.")
            .Line($"public interface I{name} : {StateMachines}.IEventSourcedStateMachine;")
            .BlankLine()
            .Summary($"Follows {model.Name} and takes the transitions the {machine.Name} state machine declares.")
            .OpenBlock($"public class {name} : {StateMachines}.EventSourcedStateMachine<{modelType}>, I{name}")
            .Line("/// <inheritdoc/>")
            .Line($"protected override global::System.Type InitialState => typeof({initial});")
            .BlankLine()
            .Line("/// <inheritdoc/>")
            .Line($"public override global::System.Collections.Immutable.IImmutableList<global::Cratis.Orleans.StateMachines.IState<{modelType}>> CreateStates() => [{string.Join(", ", machine.States.Select(state => $"new {Identifiers.ToPascalCase(state.Name)}()"))}];")
            .BlankLine()
            .Line("/// <inheritdoc/>");

        // A transition written under several states is one transition of the engine: it is tried whatever state the machine is in, and the state's allowed targets decide.
        var transitions = machine.Transitions.DistinctBy(transition => (transition.Event, transition.Guard, transition.To)).Select(transition =>
        {
            var guard = transition.Guard is null ? string.Empty : $".When(model => {SemanticLifecycleExpressions.Guard(transition.Guard, model, types)})";
            return $".On<{types.EventType(context.Events[transition.Event])}>(){guard}.TransitionTo<{Identifiers.ToPascalCase(transition.To)}>()";
        }).ToArray();
        builder.OpenBlock($"protected override void DefineTransitions({StateMachines}.IStateMachineTransitions<{modelType}> transitions)");
        if (transitions.Length > 0) builder.Line($"transitions{string.Concat(transitions)};");
        builder.EndBlock().BlankLine()
            .Line("/// <inheritdoc/>")
            .Line($"protected override global::System.Type ResolveState({modelType}? model) => InitialState;")
            .BlankLine();

        foreach (var state in machine.States)
        {
            RenderState(context, machine, state, model, modelType, self, types, builder);
        }

        builder.EndBlock();
        return new(Path.Combine(Folder(context), $"{name}.cs"), builder.ToString()) { Sources = [context.Application.Id] };
    }

    static void RenderState(SemanticApplicationContext context, SemanticStateMachine machine, SemanticState state, SemanticReadModel model, string modelType, string self, SemanticTypeSystem types, CSharpCodeBuilder builder)
    {
        var allowed = machine.Transitions.Where(transition => transition.From == state.Name).Select(transition => transition.To).Distinct(StringComparer.Ordinal).ToArray();
        var starts = state.Entering.Where(entry => entry.Kind == SemanticStateEntryKind.StartWorkflow).Select(entry => entry.Workflow!)
            .Concat(context.Application.Workflows.Where(workflow => workflow.Nodes.Any(node => node is { Kind: SemanticWorkflowNodeKind.Trigger, TriggerKind: SemanticWorkflowTriggerKind.StateEntered } && node.StateMachine == machine.Name && node.State == state.Name)).Select(workflow => workflow.Name))
            .Distinct(StringComparer.Ordinal).ToArray();
        var sends = state.Entering.Where(entry => entry.Kind == SemanticStateEntryKind.SendCommand).ToArray();
        builder.Summary($"The {state.Name} state of the {machine.Name} state machine.")
            .OpenBlock($"public sealed class {Identifiers.ToPascalCase(state.Name)} : global::Cratis.Orleans.StateMachines.State<{modelType}>")
            .Line("/// <inheritdoc/>")
            .Line($"protected override global::System.Collections.Immutable.IImmutableList<global::System.Type> AllowedTransitions => [{string.Join(", ", allowed.Select(target => $"typeof({Identifiers.ToPascalCase(target)})"))}];");
        if (starts.Length + sends.Length > 0)
        {
            builder.BlankLine().Line("/// <inheritdoc/>")
                .OpenBlock($"public override async global::System.Threading.Tasks.Task<{modelType}> OnEnter({modelType} state)");
            if (sends.Length > 0)
            {
                builder.Line("var services = ((global::Orleans.Grain)StateMachine).GrainContext.ActivationServices;")
                    .Line($"var key = (({self})StateMachine).Key.EventSourceId.Value;");
            }

            for (var index = 0; index < sends.Length; index++)
            {
                var command = context.Commands[sends[index].Command];
                var arguments = SemanticLifecycleExpressions.Arguments(command, model, types, "key", "state");
                builder.Line($"var sent{index} = await global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Cratis.Arc.Commands.ICommandPipeline>(services).Execute(new {types.SliceType(command.Id, command.Name)}({arguments}));")
                    .Line($"if (!sent{index}.IsSuccess) throw new global::System.InvalidOperationException({CSharpCodeBuilder.StringLiteral($"Entering {state.Name} in {machine.Name} sent {command.Name}, which did not succeed.")});");
            }

            for (var index = 0; index < starts.Length; index++)
            {
                builder.Line($"var started{index} = await global::Cratis.Orleans.Chronicle.Workflows.WorkflowStateExtensions.StartWorkflow(this, {CSharpCodeBuilder.StringLiteral(starts[index])});")
                    .Line($"if (!started{index}.Admitted) throw new global::System.InvalidOperationException({CSharpCodeBuilder.StringLiteral($"Entering {state.Name} in {machine.Name} could not start {starts[index]}.")});");
            }

            builder.Line("return state;").EndBlock();
        }

        builder.EndBlock().BlankLine();
    }

    static RenderedFile RenderDefinition(SemanticApplicationContext context, WorkflowGraphSpec graph)
    {
        var workflow = graph.Workflow;
        var name = Identifiers.ToPascalCase(workflow.Name);
        var model = context.ReadModels[workflow.Subject];
        var builder = context.CodeBuilder().Namespace(Namespace(context));
        var nodes = graph.Nodes.Select(node =>
            $"new({CSharpCodeBuilder.StringLiteral(node.Name)}, {Workflows}.Graphs.WorkflowNodeKind.{node.Kind}, new({CSharpCodeBuilder.StringLiteral(node.Capability)}, 1), {CSharpCodeBuilder.StringLiteral(node.ConfigurationJson)}, [{string.Join(", ", node.Outlets.Select(CSharpCodeBuilder.StringLiteral))}], new({node.MaxAttempts}, global::System.TimeSpan.FromSeconds({node.AttemptTimeout.TotalSeconds:0.###}), global::System.TimeSpan.FromSeconds({node.RetryDelay.TotalSeconds:0.###}), 1))");
        var edges = graph.Edges.Select(edge => $"new({CSharpCodeBuilder.StringLiteral(edge.From)}, {CSharpCodeBuilder.StringLiteral(edge.Outlet)}, {CSharpCodeBuilder.StringLiteral(edge.To)})");
        builder.Summary($"The {workflow.Name} workflow as a definition for the Cratis.Orleans workflow engine.")
            .OpenBlock($"public sealed class {name}Workflow : {Workflows}.Definitions.IWorkflowDefinitions")
            .Line($"static readonly {Workflows}.Definitions.WorkflowDefinitionSnapshot _definition = new(")
            .Line($"    {CSharpCodeBuilder.StringLiteral(workflow.Name)},")
            .Line($"    {CSharpCodeBuilder.StringLiteral(context.Request.Model.Application.Name)},")
            .Line("    1,")
            .Line($"    {CSharpCodeBuilder.StringLiteral(model.Name)},")
            .Line($"    new {Workflows}.Graphs.WorkflowGraph(")
            .Line($"        [{string.Join(", ", nodes)}],")
            .Line($"        [{string.Join(", ", edges)}]),")
            .Line("    \"1\",")
            .Line("    []);")
            .BlankLine()
            .Line("/// <inheritdoc/>")
            .Line($"public bool Owns({Workflows}.WorkflowKey workflow) => workflow.Workflow.Value == {CSharpCodeBuilder.StringLiteral(workflow.Name)};")
            .BlankLine()
            .Line("/// <inheritdoc/>")
            .Line($"public global::System.Threading.Tasks.Task<{Workflows}.Definitions.WorkflowDefinitionSnapshot?> GetActive({Workflows}.WorkflowKey workflow) => global::System.Threading.Tasks.Task.FromResult<{Workflows}.Definitions.WorkflowDefinitionSnapshot?>(Owns(workflow) ? _definition : null);")
            .EndBlock();
        return new(Path.Combine(Folder(context), $"{name}Workflow.cs"), builder.ToString()) { Sources = [context.Application.Id] };
    }

    static RenderedFile? RenderActions(SemanticApplicationContext context)
    {
        var commands = context.Application.Workflows.SelectMany(workflow => workflow.Nodes).Where(node => node.Kind == SemanticWorkflowNodeKind.Action)
            .Select(node => context.Commands[node.Command]).DistinctBy(command => command.Id).ToArray();
        if (commands.Length == 0) return null;
        var types = new SemanticTypeSystem(context);
        var builder = context.CodeBuilder().Namespace(Namespace(context));
        foreach (var command in commands)
        {
            var identifier = command.Properties.Single(property => property is { IsIdentifier: true, IsGenerated: false });
            var cls = $"Send{Identifiers.ToPascalCase(command.Name)}Action";
            var message = CSharpCodeBuilder.StringLiteral($"{command.Name} did not succeed.");
            builder.Summary($"Sends {command.Name} for the subject of a workflow run.")
                .OpenBlock($"public sealed class {cls}(global::Cratis.Arc.Commands.ICommandPipeline commands) : {Workflows}.Adapters.IWorkflowNodePerformer")
                .Line("/// <inheritdoc/>")
                .Line($"public {Workflows}.Graphs.CapabilityReference Capability => new({CSharpCodeBuilder.StringLiteral(WorkflowGraphSpecs.SendCapability(command.Name))}, 1);")
                .BlankLine()
                .Line("/// <inheritdoc/>")
                .OpenBlock($"public async global::System.Threading.Tasks.Task<{Workflows}.Adapters.WorkflowNodeOutcome> Perform({Workflows}.Adapters.WorkflowNodeInvocation invocation, global::System.Threading.CancellationToken cancellationToken)")
                .Line("var key = global::System.Text.Json.Nodes.JsonNode.Parse(invocation.Input.Value)?[\"trigger\"]?[\"subject\"]?[\"id\"]?.GetValue<string>() ?? string.Empty;")
                .Line($"var result = await commands.Execute(new {types.SliceType(command.Id, command.Name)}({SemanticLifecycleExpressions.Key(identifier.Type, "key", types)}));")
                .Line($"return result.IsSuccess ? {Workflows}.Adapters.WorkflowNodeOutcome.Succeeded(\"next\") : {Workflows}.Adapters.WorkflowNodeOutcome.Failed({message});")
                .EndBlock().EndBlock().BlankLine();
        }

        return new(Path.Combine(Folder(context), "WorkflowActions.cs"), builder.ToString()) { Sources = [context.Application.Id] };
    }

    static RenderedFile? RenderSignals(SemanticApplicationContext context)
    {
        var events = context.Application.Workflows.SelectMany(workflow => workflow.Nodes).Where(node => node is { Kind: SemanticWorkflowNodeKind.Wait, Duration: null })
            .Select(node => context.Events[node.Event]).DistinctBy(@event => @event.Id).ToArray();
        if (events.Length == 0) return null;
        var types = new SemanticTypeSystem(context);
        var builder = context.CodeBuilder().Namespace(Namespace(context));
        foreach (var @event in events)
        {
            var cls = $"{Identifiers.ToPascalCase(@event.Name)}WorkflowSignal";
            builder.Summary($"Resumes the workflow runs that wait for {@event.Name} about the same subject.")
                .Attribute($"global::Cratis.Chronicle.Reactors.ReactorAttribute({CSharpCodeBuilder.StringLiteral($"{context.RootNamespace}.Lifecycles.{cls}")})")
                .OpenBlock($"public class {cls}(global::Cratis.Orleans.Chronicle.Workflows.IWorkflowSignals signals) : global::Cratis.Chronicle.Reactors.IReactor")
                .Summary($"Signals the waits for {@event.Name}.")
                .OpenBlock($"public async global::System.Threading.Tasks.Task On{Identifiers.ToPascalCase(@event.Name)}({types.EventType(@event)} @event, global::Cratis.Chronicle.Events.EventContext context)")
                .Line($"await signals.Signal(new {Workflows}.WorkflowScope(context.Namespace.Value), new {Workflows}.Contracts.WorkflowCorrelationKey(context.EventSourceId.Value), new {Workflows}.Contracts.WorkflowContractReference({CSharpCodeBuilder.StringLiteral(@event.Name)}, 1), {Workflows}.Contracts.WorkflowData.Empty, {Workflows}.WorkflowActor.System);")
                .EndBlock().EndBlock().BlankLine();
        }

        return new(Path.Combine(Folder(context), "WorkflowSignals.cs"), builder.ToString()) { Sources = [context.Application.Id] };
    }

    static RenderedFile? RenderTriggers(SemanticApplicationContext context)
    {
        if (!HasEventTriggers(context)) return null;
        var types = new SemanticTypeSystem(context);
        var builder = context.CodeBuilder().Namespace(Namespace(context));
        builder.Summary("Starts workflow runs when the events their triggers name are appended.")
            .OpenBlock("public static class GeneratedWorkflows")
            .Summary("Adds the event triggers of the modeled workflows.")
            .OpenBlock("public static void Configure(global::Cratis.Orleans.Chronicle.Workflows.EventSourcedWorkflowsOptions options)");
        foreach (var workflow in context.Application.Workflows)
        {
            var model = context.ReadModels[workflow.Subject];
            foreach (var trigger in EventTriggers(workflow))
            {
                var @event = context.Events[trigger.Event];
                var source = CSharpCodeBuilder.StringLiteral($"{workflow.Name}.{trigger.Name}");
                builder.Line($"options.OnEvent<{types.EventType(@event)}>({CSharpCodeBuilder.StringLiteral(workflow.Name)}, (@event, context) => new {Workflows}.Triggers.WorkflowTrigger(")
                    .Line($"    {Workflows}.Triggers.WorkflowTriggerKind.Signal,")
                    .Line($"    new({source}),")
                    .Line("    new($\"{context.EventSourceId.Value}:{context.SequenceNumber.Value}\"),")
                    .Line($"    new {Workflows}.Contracts.WorkflowSubjectReference(new {Workflows}.Contracts.WorkflowSubjectKind({CSharpCodeBuilder.StringLiteral(model.Name)}), new(context.EventSourceId.Value)),")
                    .Line($"    {Workflows}.Contracts.WorkflowContractReference.Any,")
                    .Line($"    {Workflows}.Contracts.WorkflowData.Empty,")
                    .Line("    [],")
                    .Line("    new(context.EventSourceId.Value),")
                    .Line($"    {Workflows}.Contracts.WorkflowCausationKey.None,")
                    .Line($"    {Workflows}.WorkflowActor.System,")
                    .Line("    context.Occurred));");
            }
        }

        builder.EndBlock().EndBlock();
        return new(Path.Combine(Folder(context), "GeneratedWorkflows.cs"), builder.ToString()) { Sources = [context.Application.Id] };
    }
}
