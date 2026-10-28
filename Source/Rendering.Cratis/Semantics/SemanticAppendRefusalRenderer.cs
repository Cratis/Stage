// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders a specification that appends an event the event log refuses because of a constraint.
/// </summary>
internal static class SemanticAppendRefusalRenderer
{
    internal static RenderedFile Render(SemanticSpecification specification, SemanticConstraint constraint, SemanticApplicationContext context)
    {
        var located = context.DeclaringSlice(specification.Id);
        var behavior = SemanticSpecificationClassNames.Behavior(specification);
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var builder = context.SpecificationBuilder().Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Arc.Chronicle.Testing.Commands")
            .Using("Cratis.Specifications").Using("Xunit")
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line("readonly global::Cratis.Arc.Testing.Commands.CommandScenario<object> _scenario = new();")
            .Line(SemanticSpecificationValuesRenderer.Declarations)
            .Line("global::Cratis.Chronicle.EventSequences.AppendResult _result = null!;")
            .Line("int _givenEventCount = 0;")
            .BlankLine().OpenBlock("async global::System.Threading.Tasks.Task Establish()");
        foreach (var given in specification.GivenEvents)
        {
            var contract = context.Events[given.EventContract];
            var arguments = contract.Properties.Select(property => types.Value(given.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
            var source = types.EventSourceExpression(types.Value(given.EventSource!.Value, given.EventSource.Type), given.EventSource.Type);
            builder.Line($"await _scenario.EventScenario.Given.ForEventSource({source}).Events(new {types.EventType(contract)}({string.Join(", ", arguments)}));");
        }

        builder.Line("_givenEventCount = _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess);")
            .EndBlock().BlankLine().OpenBlock("async global::System.Threading.Tasks.Task Because()");
        var append = specification.WhenAppended!;
        var appended = context.Events[append.EventContract];
        var appendArguments = appended.Properties.Select(property => types.Value(append.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
        var appendSource = types.EventSourceExpression(types.Value(append.EventSource!.Value, append.EventSource.Type), append.EventSource.Type);
        builder.Line($"_result = await _scenario.EventLog.Append({appendSource}, new {types.EventType(appended)}({string.Join(", ", appendArguments)}));")
            .EndBlock().BlankLine()
            .Line("[global::Xunit.FactAttribute] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();")
            .Line($"[global::Xunit.FactAttribute] void should_report_the_constraint_violation() => _result.ConstraintViolations.Any(violation => violation.ConstraintName == {CSharpCodeBuilder.StringLiteral(constraint.Name)}).ShouldBeTrue();");
        if (specification.ThenErrors[0].Message is { } message)
        {
            builder.Line($"[global::Xunit.FactAttribute] void should_report_the_expected_message() => _result.ConstraintViolations.Any(violation => violation.Message == {CSharpCodeBuilder.StringLiteral(message)}).ShouldBeTrue();");
        }

        builder.Line("[global::Xunit.FactAttribute] void should_not_append_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(_givenEventCount);")
            .BlankLine().ExpressionMember("public void Dispose()", "_scenario.Dispose()").EndBlock();

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]), types.SpecificationContent(builder));
    }
}
