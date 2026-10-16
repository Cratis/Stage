// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_checking_read_backed_requirements : Specification
{
    [Theory]
    [InlineData("stored", true)]
    [InlineData("", false)]
    [InlineData(null, true)]
    public void should_resolve_the_view_property_or_semantic_null(string? value, bool expected)
    {
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan())).Plan!;
        var command = plan.Commands.Values.Single(command => command.Name == "Consume");
        var read = command.Reads.Single();
        var property = plan.ReadModels[read.ReadModel].Properties.Single(property => property.Name == "value");
        var stored = new Dictionary<SemanticId, SemanticValue>();
        if (value is not null) stored.Add(property.Id, SemanticValue.Text(value));
        var reads = new Dictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>(StringComparer.Ordinal) { [read.Name] = stored };
        var values = new Dictionary<SemanticId, SemanticValue>();
        var condition = command.Requirements.Single().Condition;
        SemanticRuleEvaluation.Condition(condition, values, reads).ShouldEqual(expected);
        var mirrored = new SemanticComparison(
            new(default, SemanticValue.Text("")),
            SemanticComparisonOperator.NotEqual,
            new(property.Id, null) { Read = read.Name });
        SemanticRuleEvaluation.Condition(mirrored, values, reads).ShouldEqual(expected);
        SemanticRuleEvaluation.Condition(new SemanticLogicalCondition(condition, SemanticLogicalOperator.And, mirrored), values, reads).ShouldEqual(expected);
        SemanticRunContext.Evaluate(new SemanticReadPathExpression(read.Name, property.Id), [], reads)
            .ShouldEqual(value is null ? SemanticValue.Null : SemanticValue.Text(value));
    }
}
