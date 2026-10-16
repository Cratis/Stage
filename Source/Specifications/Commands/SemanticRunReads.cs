// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Comparison;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.Commands;

internal sealed class SemanticRunReads(SemanticRuntimeTypes types)
{
    readonly Dictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>> _values = new(StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>> Values => _values;

    internal IEnumerable<SemanticValidationFailure> Failures(SemanticRunContext context)
    {
        var application = context.Plan.Model.Application;
        var command = context.Command;
        var rules = SemanticRuleEvaluation.RuleFailures(command, context.Specification, application.Concepts, application.Types).ToArray();
        if (!command.Reads.IsDefaultOrEmpty)
        {
            // Protected requirements must never run before property/concept rules and required presence.
            if (rules.Length > 0) return rules;
            if (Resolve(context) is { } missing) return [missing];
        }

        return rules.Concat(SemanticRuleEvaluation.RequirementFailures(command, context.Specification, _values));
    }

    SemanticValidationFailure? Resolve(SemanticRunContext context)
    {
        _values.Clear();
        var defaults = new Defaults(new SemanticClientArtifactsProvider([.. context.Plan.Events.Keys.Select(types.For)]));
        var inputs = context.Specification.When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        foreach (var read in context.Command.Reads)
        {
            var key = inputs[read.Key];
            var instance = SemanticRunProjections.ForReadModel(context.Plan, read.ReadModel, context, types, defaults)
                .FirstOrDefault(instance => SemanticExpectationComparer.AreEqual(instance.Key, key));
            if (instance is null)
            {
                return new($"{context.Plan.ReadModels[read.ReadModel].Name} '{((SemanticTextValue)key).Value}' does not exist.", SemanticValidationSeverity.Error);
            }
            _values.Add(read.Name, instance.Values.ToDictionary(value => value.TargetProperty, value => value.Value));
        }

        return null;
    }
}
