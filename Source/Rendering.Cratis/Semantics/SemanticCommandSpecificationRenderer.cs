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
internal static partial class SemanticCommandSpecificationRenderer
{
    /// <summary>
    /// Renders the command outcome of one semantic specification.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="reaction">The optional reactor prelude.</param>
    /// <returns>The generated specification source.</returns>
    public static RenderedFile Render(SemanticSpecification specification, SemanticApplicationContext context, SemanticReactionSpecificationPrelude? reaction = null)
    {
        var when = specification.When!;
        var command = context.Commands[when.Command];
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var generated = new SemanticGeneratedCommandSpecification(specification, command, types, context);
        var behavior = SemanticSpecificationClassNames.Behavior(specification);
        var ownNamespace = $"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}";
        var builder = context.SpecificationBuilder()
            .Namespace(ownNamespace)
            .Using("Cratis.Arc.Commands")
            .Using("Cratis.Arc.Testing.Commands")
            .Using("Cratis.Specifications")
            .Using("Xunit");
        AddInputUsings(builder, specification, command, context, located, generated);

        var commandName = types.SliceType(command.Id, command.Name);
        var arguments = generated.Arguments();
        var scope = new SpecificationScope(builder, specification, command, context, types, SeedsEventLog(specification, command, context), commandName, arguments) { ReactionPrelude = reaction };

        RenderHeader(scope, behavior);
        RenderEstablish(scope, generated);
        RenderBecause(scope);
        RenderOutcome(scope, generated);

        builder.BlankLine();
        if (reaction is null)
        {
            builder.ExpressionMember("public void Dispose()", "_scenario.Dispose()");
        }
        else
        {
            SemanticReactionSpecificationRenderer.RenderDispose(builder);
        }
        builder.EndBlock();
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);

        // Decided from the rendered content rather than predicted, the same way the non-semantic renderer
        // does it: only a value that renders as a culture-invariant parse needs the namespace, and emitting
        // it regardless leaves a using nobody uses in every generated specification.
        var content = types.SpecificationContent(builder);
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = types.SpecificationContent(builder.Using("System.Globalization"));
        }

        return new(path, content);
    }

    static void AddInputUsings(
        CSharpCodeBuilder builder,
        SemanticSpecification specification,
        SemanticCommand command,
        SemanticApplicationContext context,
        LocatedSemanticSlice located,
        SemanticGeneratedCommandSpecification generated)
    {
        if (command.Properties.Any(generated.NeedsCommon))
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
    }

    static bool SeedsEventLog(SemanticSpecification specification, SemanticCommand command, SemanticApplicationContext context)
    {
        // Arc selects decision mode per assembly, not only for commands with reads.
        // Preserve the reads path: routed facts use the log; other facts use the scenario's own Given.
        if (!command.Reads.IsDefaultOrEmpty) return specification.GivenEvents.Any(given => given.Route is not null);

        var constrainedEvents = context.Constraints.Select(_ => _.Constraint)
            .SelectMany(constraint => constraint.Targets.Select(target => target.EventContract).Concat(constraint.ReleasedBy)).ToHashSet();
        return !specification.GivenEvents.IsEmpty &&
            (specification.GivenEvents.Any(given => given.Route is not null || constrainedEvents.Contains(given.EventContract)) ||
             command.Produces.Any(produced => constrainedEvents.Contains(produced.EventContract)) ||
             SemanticGeneratedFreshness.Destinations(command, context).Any());
    }

    static void RenderHeader(SpecificationScope scope, string behavior)
    {
        // The scenario owns a service provider and disposes it, so the specification that owns the scenario
        // has to dispose it in turn. Generated code is built in someone else's repository, frequently with
        // analysis as errors, and an undisposed owned resource is a build failure there that they cannot fix
        // by editing the file.
        var builder = scope.Builder;
        scope.Context.Docs(scope.Specification.Id).Render(builder)
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line($"readonly global::Cratis.Arc.Testing.Commands.CommandScenario<{scope.CommandName}> _scenario = new();")
            .Line(SemanticSpecificationValuesRenderer.Declarations)
            .Line("global::Cratis.Arc.Commands.CommandResult _result = null!;");
        if (scope.CountsGivenEvents)
        {
            builder.Line("int _givenEventCount = 0;");
        }

        if (scope.ReactionPrelude is { } reaction)
        {
            SemanticReactionSpecificationRenderer.RenderHeader(scope.Builder, reaction, scope.Context);
        }
        builder.BlankLine();
    }

    static void RenderEstablish(SpecificationScope scope, SemanticGeneratedCommandSpecification generated)
    {
        var builder = scope.Builder;
        var specification = scope.Specification;
        var requiresEstablish = RequiresEstablish(scope, generated);
        if (requiresEstablish)
        {
            builder.OpenBlock(scope.SeedInLog ? "async global::System.Threading.Tasks.Task Establish()" : "void Establish()");
            generated.RenderClock(builder);
            RenderPolicyRegistration(scope);
        }

        generated.RenderFixtures(builder);

        foreach (var given in specification.GivenEvents)
        {
            RenderGivenEvent(scope, given);
        }

        if (requiresEstablish)
        {
            if (scope.CountsGivenEvents)
            {
                builder.Line("_givenEventCount = _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess);");
            }

            builder.EndBlock().BlankLine();
        }
    }

    static bool RequiresEstablish(SpecificationScope scope, SemanticGeneratedCommandSpecification generated) =>
        generated.RequiresEstablish || (scope.ReactionPrelude is not null && scope.Command.Authorization is not null);

    static void RenderPolicyRegistration(SpecificationScope scope)
    {
        if (scope.Command.Authorization is not null)
        {
            scope.Builder.Line($"global::{scope.Context.RootNamespace}.GeneratedPolicies.Registration.Register(_scenario.Services);");
        }
    }

    static void RenderGivenEvent(SpecificationScope scope, SemanticSpecificationEvent given)
    {
        var builder = scope.Builder;
        var context = scope.Context;
        var types = scope.Types;
        var @event = context.Events[given.EventContract];
        var source = given.EventSource;
        if (SourceNeedsCommon(source) || PropertyValuesNeedCommon(@event.Properties, given.Values))
        {
            builder.Using(context.CommonNamespace);
        }

        var eventArguments = @event.Properties.Select(property =>
            types.Value(given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
        var eventSource = SemanticSpecificationEventSources.Expression(context, types, given);
        var eventValue = $"new {types.EventType(@event)}({string.Join(", ", eventArguments)})";
        if (given.Route is { } route)
        {
            builder.Line($"(await _scenario.EventLog.Append({eventSource}, {eventValue}, {SemanticSpecificationRouteRendering.Arguments(route, context, types)})).IsSuccess.ShouldBeTrue();");
            return;
        }

        // EventScenario is unavailable in decision mode, which Arc now selects per assembly; the checked log append works in both modes.
        builder.Line(scope.SeedInLog && scope.Command.Reads.IsDefaultOrEmpty
            ? $"(await _scenario.EventLog.Append({eventSource}, {eventValue})).IsSuccess.ShouldBeTrue();"
            : $"_scenario.Given.ForEventSource({eventSource}).Events({eventValue});");
    }

    static bool SourceNeedsCommon(SemanticEventSourceIdentity? source) =>
        source is not null && SemanticTypeSystem.ValueNeedsCommon(source.Value, source.Type);

    static void RenderBecause(SpecificationScope scope)
    {
        var builder = scope.Builder;
        var commandName = scope.CommandName;
        if (scope.ReactionPrelude is not null)
        {
            SemanticReactionSpecificationRenderer.RenderBecause(scope);
            return;
        }
        var caller = scope.Specification.GivenCaller;
        if (caller is null && scope.Command.Authorization is not null)
        {
            builder.Line($"async global::System.Threading.Tasks.Task Because() => _result = await _scenario.Execute(new {commandName}({string.Join(", ", scope.Arguments)}));")
                .BlankLine();
            return;
        }

        // A command without modeled authorization renders [Authorize], so a specification without a caller runs as the default authenticated caller.
        var claims = caller is null ? [] : caller.Roles.Select(role => $"new global::System.Security.Claims.Claim(global::System.Security.Claims.ClaimTypes.Role, {CSharpCodeBuilder.StringLiteral(role)})")
            .Concat(caller.Claims.Select(claim => $"new global::System.Security.Claims.Claim({CSharpCodeBuilder.StringLiteral(claim.Type)}, {CSharpCodeBuilder.StringLiteral(claim.Value)})"));
        var authentication = caller?.Authenticated != false ? "\"Screenplay\"" : "null";
        builder.OpenBlock("async global::System.Threading.Tasks.Task Because()");
        if (caller is null)
        {
            builder.Line(SemanticDefaultCaller.Comment);
        }
        builder
            .Line($"var principal = new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([{string.Join(", ", claims)}], {authentication}));")
            .Line("var principalOverride = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor());")
            .Line("using var scope = principalOverride.BeginScope(principal);")
            .Line($"_result = await _scenario.Execute(new {commandName}({string.Join(", ", scope.Arguments)}));")
            .EndBlock()
            .BlankLine();
    }

    static void RenderOutcome(SpecificationScope scope, SemanticGeneratedCommandSpecification generated)
    {
        if (scope.Specification.ThenDenied)
        {
            RenderDenied(scope);
        }
        else if (!scope.Specification.ThenErrors.IsEmpty)
        {
            RenderRejected(scope);
        }
        else
        {
            generated.RenderAccepted(scope.Builder, () => RenderAccepted(scope));
        }
    }

    static void RenderDenied(SpecificationScope scope)
    {
        scope.Builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
            .Line("[global::Xunit.FactAttribute] void should_be_denied() => _result.IsAuthorized.ShouldBeFalse();")
            .Line("[global::Xunit.FactAttribute] void should_not_append_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(_givenEventCount);");
    }

    static void RenderRejected(SpecificationScope scope)
    {
        var builder = scope.Builder;
        builder.Line("[global::Xunit.FactAttribute] void should_not_succeed() => _result.ShouldNotBeSuccessful();")
            .Line("[global::Xunit.FactAttribute] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();");
        if (scope.Specification.ThenErrors[0].Message is { } message)
        {
            builder.Line(ExpectedErrorAssertion(scope, message));
        }

        builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
            .Line("[global::Xunit.FactAttribute] void should_not_append_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(_givenEventCount);");
        RenderConstraintViolation(builder, scope.Specification, scope.Context);
    }

    static string ExpectedErrorAssertion(SpecificationScope scope, string message)
    {
        if (scope.Context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15) && message == SemanticGenerationShapes.FreshnessMessage)
        {
            return "[global::Xunit.FactAttribute] void should_report_the_freshness_violation() => _result.ValidationResults.First().Reason.ShouldEqual(global::Cratis.Arc.Validation.ValidationResultReason.ConcurrencyViolation);";
        }

        return message.StartsWith("$strings.", StringComparison.Ordinal)
            ? $"[global::Xunit.FactAttribute] void should_report_the_expected_first_error_key() => _result.ValidationResults.First().State.ShouldEqual({CSharpCodeBuilder.StringLiteral(message)});"
            : $"[global::Xunit.FactAttribute] void should_report_the_expected_first_error() => _result.ValidationResults.First().Message.ShouldEqual({CSharpCodeBuilder.StringLiteral(message)});";
    }

    static void RenderConstraintViolation(CSharpCodeBuilder builder, SemanticSpecification specification, SemanticApplicationContext context)
    {
        if (SemanticSpecificationAdmission.ConstraintName(context, specification) is { } constraintName)
        {
            builder.Line($"[global::Xunit.FactAttribute] void should_report_the_constraint_violation() => _result.ShouldHaveConstraintViolationFor({CSharpCodeBuilder.StringLiteral(constraintName)});");
        }
    }

    static bool PropertyValuesNeedCommon(IEnumerable<SemanticProperty> properties, IEnumerable<SemanticPropertyValue> values) =>
        properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
            values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));

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
}
