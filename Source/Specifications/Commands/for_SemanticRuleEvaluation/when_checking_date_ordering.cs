// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_checking_date_ordering : Specification
{
    public static TheoryData<string, string, string, string?, bool, bool> Cases
    {
        get
        {
            var cases = new TheoryData<string, string, string, string?, bool, bool>();
            foreach (var operation in new[] { "==", "!=", ">", ">=", "<", "<=" })
            {
                foreach (var concept in new[] { false, true })
                {
                    foreach (var value in new string?[] { "2026-01-01", "2026-01-02", "2025-12-31", null })
                        cases.Add("Date", operation, "2026-01-01", value, concept, value is null);
                    foreach (var value in new string?[] { "2026-01-01T01:00:00.0000000+01:00", "2026-01-01T00:00:00.0000001Z", "2025-12-31T23:59:59.9999999Z", "2025-12-31T19:00:00.0000000-05:00", "2026-01-01T00:00:00.0000000Z", null })
                        cases.Add("DateTime", operation, "2026-01-01T00:00:00Z", value, concept, value is null);
                }
            }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void should_match_reference_rules_and_default_messages(string primitive, string operation, string threshold, string? value, bool concept, bool optional)
    {
        var source = $$"""
            concept Moment : {{primitive}}
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at {{(concept ? "Moment" : primitive)}}{{(optional ? " optional" : "")}}
                    validate
                      at {{operation}} "{{threshold}}"
                    produces Kept
                      for id
                  event Kept
            """;
        var identity = ApplicationIdentity.Create("DateRules");
        var catalog = SemanticIdentityCatalog.Empty(identity);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("DateRules", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var command = plan.Commands.Values.Single();
        var input = value is null ? SemanticValue.Null : SemanticValue.Text(value);
        var values = command.Properties.Select(property => new SemanticPropertyValue(property.Id, property.Name == "id" ? SemanticValue.Text("window") : input)).ToImmutableArray();
        var fixture = new SemanticSpecification(default, "Rules", [], [], new(command.Id, values), [], [], [], []);
        var failures = SemanticRuleEvaluation.RuleFailures(command, fixture, plan.Model.Application).ToArray();
        var reference = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, SemanticExecutionRequest.Create(command.Id, values, []));
        if (reference is SemanticRejected rejected)
        {
            rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
            failures.Single().Message.ShouldEqual(rejected.Details);
        }
        else
        {
            reference.ShouldBeOfExactType<SemanticAccepted>();
            failures.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData("==", true)]
    [InlineData("!=", false)]
    [InlineData(">", false)]
    [InlineData(">=", true)]
    [InlineData("<", false)]
    [InlineData("<=", true)]
    public void should_use_instant_equality_only_in_conditions(string operation, bool expected)
    {
        var id = SemanticId.Create(SemanticKind.Property, "date");
        var comparison = Enum.Parse<SemanticComparisonOperator>(operation switch
        {
            "==" => "Equal", "!=" => "NotEqual", ">" => "GreaterThan", ">=" => "GreaterThanOrEqual", "<" => "LessThan", _ => "LessThanOrEqual"
        });
        var condition = new SemanticComparison(new(id, null), comparison, new(default, SemanticValue.Text("2026-01-01T00:00:00Z")));
        var values = new Dictionary<SemanticId, SemanticValue> { [id] = SemanticValue.Text("2026-01-01T01:00:00+01:00") };
        var types = new Dictionary<SemanticId, SemanticPrimitiveType> { [id] = SemanticPrimitiveType.DateTime };
        SemanticRuleEvaluation.Condition(condition, values, dateTypes: types).ShouldEqual(expected);
    }
}
