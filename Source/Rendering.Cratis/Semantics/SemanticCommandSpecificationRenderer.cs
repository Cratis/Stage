// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders command acceptance and rejection specifications directly from ESM.
/// </summary>
internal static class SemanticCommandSpecificationRenderer
{
    /// <summary>
    /// Renders the command outcome of one semantic specification.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated specification source.</returns>
    public static RenderedFile Render(SemanticSpecification specification, SemanticApplicationContext context)
    {
        var when = specification.When!;
        var command = context.Commands[when.Command];
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context);
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}";
        var ownNamespace = $"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}";
        var builder = new CSharpCodeBuilder()
            .Namespace(ownNamespace)
            .Using("Cratis.Arc.Commands")
            .Using("Cratis.Arc.Testing.Commands")
            .Using("Cratis.Specifications")
            .Using("Xunit");
        if (command.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
            when.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type)))
        {
            builder.Using(context.CommonNamespace);
        }

        if (specification.GivenCaller is not null)
        {
            builder.Using("Cratis.Arc.Authorization")
                .Using("Cratis.Arc.Http")
                .Using("System.Security.Claims");
        }

        if (!specification.GivenEvents.IsEmpty)
        {
            builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
                .Using("Cratis.Chronicle.Events");
        }

        foreach (var expected in specification.GivenEvents.Concat(specification.ThenEvents))
        {
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(expected.EventContract).Path);
            if (!string.Equals(eventNamespace, SliceNaming.Namespace(context.RootNamespace, located.Path), StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }
        }

        var commandName = types.SliceType(command.Id, command.Name);
        var arguments = command.Properties.Select(property =>
            types.Value(when.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));

        var constrainedEvents = context.Constraints.Select(_ => _.Constraint)
            .SelectMany(constraint => constraint.Targets.Select(target => target.EventContract).Concat(constraint.ReleasedBy)).ToHashSet();
        var seedInLog = !specification.GivenEvents.IsEmpty &&
            (specification.GivenEvents.Any(given => given.Route is not null || constrainedEvents.Contains(given.EventContract)) ||
             command.Produces.Any(produced => constrainedEvents.Contains(produced.EventContract)));

        // The scenario owns a service provider and disposes it, so the specification that owns the scenario
        // has to dispose it in turn. Generated code is built in someone else's repository, frequently with
        // analysis as errors, and an undisposed owned resource is a build failure there that they cannot fix
        // by editing the file.
        context.Docs(specification.Id).Render(builder)
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line($"readonly global::Cratis.Arc.Testing.Commands.CommandScenario<{commandName}> _scenario = new();")
            .Line("global::Cratis.Arc.Commands.CommandResult _result = null!;");
        if (seedInLog || specification.ThenDenied || !specification.ThenErrors.IsEmpty)
        {
            builder.Line("int _givenEventCount = 0;");
        }

        builder.BlankLine();

        if (!specification.GivenEvents.IsEmpty || specification.GivenCaller is not null)
        {
            builder.OpenBlock(seedInLog ? "async global::System.Threading.Tasks.Task Establish()" : "void Establish()");
            if (command.Authorization is not null)
            {
                builder.Line($"global::{context.RootNamespace}.GeneratedPolicies.Registration.Register(_scenario.Services);");
            }
        }

        foreach (var given in specification.GivenEvents)
        {
            var @event = context.Events[given.EventContract];
            var source = given.EventSource!;
            if (SemanticTypeSystem.ValueNeedsCommon(source.Value, source.Type) ||
                @event.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
                    given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type)))
            {
                builder.Using(context.CommonNamespace);
            }

            var eventArguments = @event.Properties.Select(property =>
                types.Value(given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            var eventSource = types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type);
            var eventValue = $"new {types.EventType(@event)}({string.Join(", ", eventArguments)})";
            if (given.Route is { } route)
            {
                builder.Line($"(await _scenario.EventLog.Append({eventSource}, {eventValue}, {SemanticSpecificationRouteRendering.Arguments(route, context)})).IsSuccess.ShouldBeTrue();");
            }
            else
            {
                builder.Line(seedInLog
                    ? $"await _scenario.EventScenario.Given.ForEventSource({eventSource}).Events({eventValue});"
                    : $"_scenario.Given.ForEventSource({eventSource}).Events({eventValue});");
            }
        }

        if (!specification.GivenEvents.IsEmpty || specification.GivenCaller is not null)
        {
            if (seedInLog || specification.ThenDenied || !specification.ThenErrors.IsEmpty)
            {
                builder.Line("_givenEventCount = _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess);");
            }

            builder.EndBlock().BlankLine();
        }

        if (specification.GivenCaller is { } caller)
        {
            var claims = caller.Roles.Select(role => $"new global::System.Security.Claims.Claim(global::System.Security.Claims.ClaimTypes.Role, {CSharpCodeBuilder.StringLiteral(role)})")
                .Concat(caller.Claims.Select(claim => $"new global::System.Security.Claims.Claim({CSharpCodeBuilder.StringLiteral(claim.Type)}, {CSharpCodeBuilder.StringLiteral(claim.Value)})"));
            var authentication = caller.Authenticated ? "\"Screenplay\"" : "null";
            builder.OpenBlock("async global::System.Threading.Tasks.Task Because()")
                .Line($"var principal = new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([{string.Join(", ", claims)}], {authentication}));")
                .Line("var principalOverride = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor());")
                .Line("using var scope = principalOverride.BeginScope(principal);")
                .Line($"_result = await _scenario.Execute(new {commandName}({string.Join(", ", arguments)}));")
                .EndBlock()
                .BlankLine();
        }
        else
        {
            builder.Line($"async global::System.Threading.Tasks.Task Because() => _result = await _scenario.Execute(new {commandName}({string.Join(", ", arguments)}));")
                .BlankLine();
        }

        if (specification.ThenDenied)
        {
            var policy = $"global::{context.PoliciesNamespace}.{Policies.SemanticPolicyArtifactRenderer.Name(command.Id)}";
            var policyName = context.Domain.Count == 0 ? $"nameof({policy})" : CSharpCodeBuilder.StringLiteral(Policies.SemanticPolicyArtifactRenderer.PolicyName(context, command.Id));
            builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
                .Using("Cratis.Arc.Authorization")
                .Using("Microsoft.Extensions.DependencyInjection")
                .Line("[global::Xunit.FactAttribute] void should_be_denied() => _result.IsAuthorized.ShouldBeFalse();")
                .Line("[global::Xunit.FactAttribute] void should_not_append_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(_givenEventCount);")
                .OpenBlock("[global::Xunit.FactAttribute] void should_resolve_the_registered_policy()")
                .Line($"_scenario.Services.Any(registration => registration.ImplementationInstance is global::Cratis.Arc.Authorization.AuthorizationPolicyRegistration policy && policy.Name == {policyName} && policy.PolicyType == typeof({policy})).ShouldBeTrue();")
                .Line("using var provider = _scenario.Services.BuildServiceProvider();")
                .Line("using var scope = provider.CreateScope();")
                .Line($"scope.ServiceProvider.GetRequiredService<{policy}>().ShouldNotBeNull();")
                .EndBlock();
        }
        else if (!specification.ThenErrors.IsEmpty)
        {
            builder.Line("[global::Xunit.FactAttribute] void should_not_succeed() => _result.ShouldNotBeSuccessful();")
                .Line("[global::Xunit.FactAttribute] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();");
            if (specification.ThenErrors[0].Message is { } message)
            {
                builder.Line(message.StartsWith("$strings.", StringComparison.Ordinal)
                    ? $"[global::Xunit.FactAttribute] void should_report_the_expected_first_error_key() => _result.ValidationResults.First().State.ShouldEqual({CSharpCodeBuilder.StringLiteral(message)});"
                    : $"[global::Xunit.FactAttribute] void should_report_the_expected_first_error() => _result.ValidationResults.First().Message.ShouldEqual({CSharpCodeBuilder.StringLiteral(message)});");
            }

            builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
                .Line("[global::Xunit.FactAttribute] void should_not_append_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(_givenEventCount);");
            RenderConstraintViolation(builder, specification, context);
        }
        else
        {
            RenderAccepted(builder, specification, command, context, types, seedInLog);
        }

        builder.BlankLine()
            .ExpressionMember("public void Dispose()", "_scenario.Dispose()")
            .EndBlock();
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);

        // Decided from the rendered content rather than predicted, the same way the non-semantic renderer
        // does it: only a value that renders as a culture-invariant parse needs the namespace, and emitting
        // it regardless leaves a using nobody uses in every generated specification.
        var content = builder.ToString();
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = builder.Using("System.Globalization").ToString();
        }

        return new(path, Conditional(content));
    }

    static void RenderConstraintViolation(CSharpCodeBuilder builder, SemanticSpecification specification, SemanticApplicationContext context)
    {
        if (SemanticSpecificationAdmission.ConstraintName(context, specification) is { } constraintName)
        {
            builder.Line($"[global::Xunit.FactAttribute] void should_report_the_constraint_violation() => _result.ShouldHaveConstraintViolationFor({CSharpCodeBuilder.StringLiteral(constraintName)});");
        }
    }

    static void RenderAccepted(
        CSharpCodeBuilder builder,
        SemanticSpecification specification,
        SemanticCommand command,
        SemanticApplicationContext context,
        SemanticTypeSystem types,
        bool seedInLog)
    {
        builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
            .Using("Cratis.Chronicle.Events");
        foreach (var (expected, index) in specification.ThenEvents.Select((value, index) => (value, index)))
        {
            foreach (var property in context.Events[expected.EventContract].Properties.Where(property => property.Type.IsCollection))
            {
                var value = expected.Values.Single(_ => _.TargetProperty == property.Id).Value;
                if (value is SemanticArrayValue)
                {
                    var elementType = property.Type with { IsCollection = false, IsOptional = false };
                    if (SemanticTypeSystem.DeclarationNeedsCommon(elementType))
                    {
                        builder.Using(context.CommonNamespace);
                    }

                    builder.Line($"static readonly {types.Type(elementType)}[] {ExpectedCollectionName(property, index)} = {types.Value(value, property.Type)};");
                }
            }
        }

        builder.Line("[global::Xunit.FactAttribute] void should_succeed() => _result.ShouldBeSuccessful();");
        if (specification.ThenEvents.Length > 1)
        {
            builder.Line(seedInLog
                ? $"[global::Xunit.FactAttribute] void should_append_exactly_{specification.ThenEvents.Length}_events() => (_scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess) - _givenEventCount).ShouldEqual({specification.ThenEvents.Length});"
                : $"[global::Xunit.FactAttribute] void should_append_exactly_{specification.ThenEvents.Length}_events() => _scenario.AppendedEvents.Count.ShouldEqual({specification.ThenEvents.Length});");
        }

        if (seedInLog && specification.ThenEvents.Length == 1)
        {
            builder.Line("[global::Xunit.FactAttribute] void should_append_exactly_one_new_event() => (_scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess) - _givenEventCount).ShouldEqual(1);");
        }

        if (specification.ThenEventsInAnyOrder &&
            (specification.ThenEvents.Length > 1 || context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V8)))
        {
            RenderUnorderedEvents(builder, specification, command, context, types, seedInLog);
            return;
        }

        foreach (var (expected, index) in specification.ThenEvents.Select((value, index) => (value, index)))
        {
            var @event = context.Events[expected.EventContract];
            var produced = command.Produces[index];
            var source = SemanticDestinations.ForSpecification(specification, command, produced);
            if (SemanticTypeSystem.ValueNeedsCommon(source.Value, source.Type) ||
                @event.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
                    expected.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type)))
            {
                builder.Using(context.CommonNamespace);
            }

            var predicate = @event.Properties.IsEmpty ? "true" : string.Join(" && ", @event.Properties.Select(property =>
            {
                var value = expected.Values.Single(_ => _.TargetProperty == property.Id).Value;
                return EventPropertyPredicate(property, value, types, index);
            }));
            var sourceValue = types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type);
            var name = $"should_have_appended_{Identifiers.ToSnakeCase(@event.Name)}";
            if (specification.ThenEvents.Length == 1 && !seedInLog && expected.Route is null && !expected.Unrouted)
            {
                builder.Line($"[global::Xunit.FactAttribute] async global::System.Threading.Tasks.Task {name}() => await _scenario.ShouldHaveAppendedEvent<{types.SliceType(command.Id, command.Name)}, {types.EventType(@event)}>({sourceValue}, @event => {predicate});");
            }
            else
            {
                var appended = seedInLog
                    ? $"_scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).ToArray()[_givenEventCount + {index}]"
                    : $"_scenario.AppendedEvents[{index}]";
                var route = SemanticSpecificationRouteRendering.Predicate(expected, context, appended);
                builder.Line($"[global::Xunit.FactAttribute] void {name}_at_position_{index + 1}() => ({appended}.Event.Context.EventSourceId == {sourceValue} && {appended}.Event.Content is {types.EventType(@event)} @event && {predicate}{route}).ShouldBeTrue();");
            }
        }
    }

    static void RenderUnorderedEvents(
        CSharpCodeBuilder builder,
        SemanticSpecification specification,
        SemanticCommand command,
        SemanticApplicationContext context,
        SemanticTypeSystem types,
        bool seedInLog)
    {
        if (context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V8))
        {
            RenderAssignedEvents(builder, specification, context, types, seedInLog);
            return;
        }

        // Preserve the pre-v8 greedy matcher byte-for-byte.
        builder.OpenBlock("[global::Xunit.FactAttribute] void should_append_the_expected_event_multiset()")
            .Line(seedInLog
                ? "var remaining = _scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).Skip(_givenEventCount).ToList();"
                : "var remaining = _scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).ToList();")
            .Line($"remaining.Count.ShouldEqual({specification.ThenEvents.Length});");
        foreach (var (expected, index) in specification.ThenEvents.Select((value, index) => (value, index)))
        {
            var @event = context.Events[expected.EventContract];
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(expected.EventContract).Path);
            builder.Using(eventNamespace);
            var predicates = @event.Properties.Select(property =>
            {
                var value = expected.Values.Single(_ => _.TargetProperty == property.Id).Value;
                if (SemanticTypeSystem.ValueNeedsCommon(value, property.Type))
                {
                    builder.Using(context.CommonNamespace);
                }

                return EventPropertyPredicate(property, value, types, index);
            });
            var produced = command.Produces.First(_ => _.EventContract == expected.EventContract);
            var identity = SemanticDestinations.ForSpecification(specification, command, produced);
            var source = $" && entry.Event.Context.EventSourceId == {types.EventSourceExpression(types.Value(identity.Value, identity.Type), identity.Type)}";
            if (SemanticTypeSystem.ValueNeedsCommon(identity.Value, identity.Type))
            {
                builder.Using(context.CommonNamespace);
            }

            builder.Line($"var match{index} = remaining.FindIndex(entry => entry.Event.Content is {types.EventType(@event)} @event{source}{string.Concat(predicates.Select(predicate => $" && {predicate}"))});")
                .Line($"(match{index} >= 0).ShouldBeTrue();")
                .Line($"remaining.RemoveAt(match{index});");
        }

        builder.EndBlock();
    }

    static void RenderAssignedEvents(
        CSharpCodeBuilder builder,
        SemanticSpecification specification,
        SemanticApplicationContext context,
        SemanticTypeSystem types,
        bool seedInLog)
    {
        builder.OpenBlock("[global::Xunit.FactAttribute] void should_append_the_expected_event_multiset()")
            .Line(seedInLog
                ? "var actual = _scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).Skip(_givenEventCount).ToArray();"
                : "var actual = _scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).ToArray();")
            .Line($"actual.Length.ShouldEqual({specification.ThenEvents.Length});")
            .Line("var assignments = global::System.Linq.Enumerable.Repeat(-1, actual.Length).ToArray();")
            .OpenBlock($"for (var expectation = 0; expectation < {specification.ThenEvents.Length}; expectation++)")
            .Line("Assign(expectation, new bool[actual.Length]).ShouldBeTrue();")
            .EndBlock().BlankLine()
            .OpenBlock("bool Assign(int expectation, bool[] visited)")
            .OpenBlock("for (var fact = 0; fact < actual.Length; fact++)")
            .Line("if (visited[fact] || !Matches(expectation, fact)) continue;")
            .Line("visited[fact] = true;")
            .OpenBlock("if (assignments[fact] < 0 || Assign(assignments[fact], visited))")
            .Line("assignments[fact] = expectation;")
            .Line("return true;")
            .EndBlock().EndBlock()
            .Line("return false;")
            .EndBlock().BlankLine()
            .OpenBlock("bool Matches(int expectation, int fact)")
            .Line("var entry = actual[fact];")
            .OpenBlock("return expectation switch");
        foreach (var (expected, index) in specification.ThenEvents.Select((value, index) => (value, index)))
        {
            var @event = context.Events[expected.EventContract];
            var predicates = @event.Properties.Select(property => EventPropertyPredicate(property, expected.Values.Single(value => value.TargetProperty == property.Id).Value, types, index));
            var sources = new[] { expected.EventSource, specification.When!.EventSource }.OfType<SemanticEventSourceIdentity>();
            var source = string.Concat(sources.Select(identity =>
                $" && entry.Event.Context.EventSourceId == {types.EventSourceExpression(types.Value(identity.Value, identity.Type), identity.Type)}"));
            if (sources.Any(identity => SemanticTypeSystem.ValueNeedsCommon(identity.Value, identity.Type)) ||
                @event.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(expected.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type)))
            {
                builder.Using(context.CommonNamespace);
            }
            var route = SemanticSpecificationRouteRendering.Predicate(expected, context, "entry");
            builder.Line($"{index} => entry.Event.Content is {types.EventType(@event)} @event{source}{string.Concat(predicates.Select(predicate => $" && {predicate}"))}{route},");
        }
        builder.Line("_ => false")
            .EndBlock().Line(";")
            .EndBlock().EndBlock();
    }

    static string ExpectedCollectionName(SemanticProperty property, int index) => $"_expected_event_{index}_{Identifiers.ToSnakeCase(property.Name)}";

    static string EventPropertyPredicate(SemanticProperty property, SemanticValue value, SemanticTypeSystem types, int index)
    {
        var name = Identifiers.ToPascalCase(property.Name);
        if (!property.Type.IsCollection)
        {
            return $"@event.{name} == {types.Value(value, property.Type)}";
        }

        if (value is SemanticNullValue)
        {
            return $"@event.{name} is null";
        }

        return $"(@event.{name} is {{ }} actual{name} && actual{name}.SequenceEqual({ExpectedCollectionName(property, index)}))";
    }

    static string Conditional(string content) => $"#if DEBUG\n{content}\n#endif\n";
}
