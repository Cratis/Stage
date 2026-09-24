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
            builder.Using($"{context.RootNamespace}.Common");
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

        var commandName = Identifiers.ToPascalCase(command.Name);
        var arguments = command.Properties.Select(property =>
            types.Value(when.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));

        var constrainedEvents = context.Constraints.Select(_ => _.Constraint)
            .SelectMany(constraint => constraint.Targets.Select(target => target.EventContract).Concat(constraint.ReleasedBy)).ToHashSet();
        var seedInLog = !specification.GivenEvents.IsEmpty &&
            (specification.GivenEvents.Any(given => constrainedEvents.Contains(given.EventContract)) ||
             command.Produces.Any(produced => constrainedEvents.Contains(produced.EventContract)));

        // The scenario owns a service provider and disposes it, so the specification that owns the scenario
        // has to dispose it in turn. Generated code is built in someone else's repository, frequently with
        // analysis as errors, and an undisposed owned resource is a build failure there that they cannot fix
        // by editing the file.
        builder.OpenBlock($"public class {behavior} : Specification, IDisposable")
            .Line($"readonly CommandScenario<{commandName}> _scenario = new();")
            .Line("CommandResult _result = null!;");
        if (seedInLog || specification.ThenDenied || !specification.ThenErrors.IsEmpty)
        {
            builder.Line("int _givenEventCount = 0;");
        }

        builder.BlankLine();

        if (!specification.GivenEvents.IsEmpty || specification.GivenCaller is not null)
        {
            builder.OpenBlock(seedInLog ? "async Task Establish()" : "void Establish()");
            if (command.Authorization is not null)
            {
                builder.Line($"{context.RootNamespace}.GeneratedPolicies.Registration.Register(_scenario.Services);");
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
                builder.Using($"{context.RootNamespace}.Common");
            }

            var eventArguments = @event.Properties.Select(property =>
                types.Value(given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            var eventSource = types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type);
            var eventValue = $"new {Identifiers.ToPascalCase(@event.Name)}({string.Join(", ", eventArguments)})";
            builder.Line(seedInLog
                ? $"await _scenario.EventScenario.Given.ForEventSource({eventSource}).Events({eventValue});"
                : $"_scenario.Given.ForEventSource({eventSource}).Events({eventValue});");
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
            var claims = caller.Roles.Select(role => $"new Claim(ClaimTypes.Role, {CSharpCodeBuilder.StringLiteral(role)})")
                .Concat(caller.Claims.Select(claim => $"new Claim({CSharpCodeBuilder.StringLiteral(claim.Type)}, {CSharpCodeBuilder.StringLiteral(claim.Value)})"));
            var authentication = caller.Authenticated ? "\"Screenplay\"" : "null";
            builder.OpenBlock("async Task Because()")
                .Line($"var principal = new ClaimsPrincipal(new ClaimsIdentity([{string.Join(", ", claims)}], {authentication}));")
                .Line("var principalOverride = new CurrentPrincipalAccessor(new HttpRequestContextAccessor());")
                .Line("using var scope = principalOverride.BeginScope(principal);")
                .Line($"_result = await _scenario.Execute(new {commandName}({string.Join(", ", arguments)}));")
                .EndBlock()
                .BlankLine();
        }
        else
        {
            builder.Line($"async Task Because() => _result = await _scenario.Execute(new {commandName}({string.Join(", ", arguments)}));")
                .BlankLine();
        }

        if (specification.ThenDenied)
        {
            var policy = $"{context.RootNamespace}.GeneratedPolicies.StagePolicy_{command.Id.ToString().Replace('-', '_').Replace(':', '_')}";
            builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
                .Using("Cratis.Arc.Authorization")
                .Using("Microsoft.Extensions.DependencyInjection")
                .Line("[Fact] void should_be_denied() => _result.IsAuthorized.ShouldBeFalse();")
                .Line("[Fact] void should_not_append_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(_givenEventCount);")
                .OpenBlock("[Fact] void should_resolve_the_registered_policy()")
                .Line($"_scenario.Services.Any(registration => registration.ImplementationInstance is AuthorizationPolicyRegistration policy && policy.Name == nameof({policy}) && policy.PolicyType == typeof({policy})).ShouldBeTrue();")
                .Line("using var provider = _scenario.Services.BuildServiceProvider();")
                .Line("using var scope = provider.CreateScope();")
                .Line($"scope.ServiceProvider.GetRequiredService<{policy}>().ShouldNotBeNull();")
                .EndBlock();
        }
        else if (!specification.ThenErrors.IsEmpty)
        {
            builder.Line("[Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();")
                .Line("[Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();");
            if (specification.ThenErrors[0].Message is { } message)
            {
                builder.Line($"[Fact] void should_report_the_expected_first_error() => _result.ValidationResults.First().Message.ShouldEqual({CSharpCodeBuilder.StringLiteral(message)});");
            }

            builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
                .Line("[Fact] void should_not_append_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(_givenEventCount);");
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
            builder.Line($"[Fact] void should_report_the_constraint_violation() => _result.ShouldHaveConstraintViolationFor({CSharpCodeBuilder.StringLiteral(constraintName)});");
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
            .Using("Cratis.Chronicle.Events")
            .Line("[Fact] void should_succeed() => _result.ShouldBeSuccessful();");
        if (specification.ThenEvents.Length > 1)
        {
            builder.Line(seedInLog
                ? $"[Fact] void should_append_exactly_{specification.ThenEvents.Length}_events() => (_scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess) - _givenEventCount).ShouldEqual({specification.ThenEvents.Length});"
                : $"[Fact] void should_append_exactly_{specification.ThenEvents.Length}_events() => _scenario.AppendedEvents.Count.ShouldEqual({specification.ThenEvents.Length});");
        }

        if (seedInLog && specification.ThenEvents.Length == 1)
        {
            builder.Line("[Fact] void should_append_exactly_one_new_event() => (_scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess) - _givenEventCount).ShouldEqual(1);");
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
                builder.Using($"{context.RootNamespace}.Common");
            }

            var predicate = string.Join(" && ", @event.Properties.Select(property =>
            {
                var value = expected.Values.Single(_ => _.TargetProperty == property.Id).Value;
                return $"@event.{Identifiers.ToPascalCase(property.Name)} == {types.Value(value, property.Type)}";
            }));
            var sourceValue = types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type);
            var name = $"should_have_appended_{Identifiers.ToSnakeCase(@event.Name)}";
            if (specification.ThenEvents.Length == 1 && !seedInLog)
            {
                builder.Line($"[Fact] async Task {name}() => await _scenario.ShouldHaveAppendedEvent<{Identifiers.ToPascalCase(command.Name)}, {Identifiers.ToPascalCase(@event.Name)}>({sourceValue}, @event => {predicate});");
            }
            else
            {
                var appended = seedInLog
                    ? $"_scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).ToArray()[_givenEventCount + {index}]"
                    : $"_scenario.AppendedEvents[{index}]";
                builder.Line($"[Fact] void {name}_at_position_{index + 1}() => ({appended}.Event.Context.EventSourceId == {sourceValue} && {appended}.Event.Content is {Identifiers.ToPascalCase(@event.Name)} @event && {predicate}).ShouldBeTrue();");
            }
        }
    }

    static string Conditional(string content) => $"#if DEBUG\n{content}\n#endif\n";
}
