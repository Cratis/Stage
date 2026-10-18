// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_querying_with_filters;

public class with_projected_and_seeded_rows : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;
    bool[] _reference = [];

    async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("QueryFilters"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source());
        var compilation = new SemanticModelCompiler().Compile("QueryFilters", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());
        _reference = [.. _report.Results.Select(result => new SemanticSpecificationRunner().Run(_plan, SemanticId.Parse(result.SpecificationId)).Passed)];
    }

    [Fact] void should_execute_all_cases() => _report.Results.Count.ShouldEqual(9);
    [Fact] void should_match_reference_outcomes() => _report.Results.Select(result => result.Outcome == SemanticSpecificationOutcome.Passed).ShouldEqual(_reference);
    [Fact] void should_pass_every_correct_expectation() => Assert.All(_report.Results.Where(result => !result.Name.StartsWith("Wrong", StringComparison.Ordinal)), result => Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Name}: {result.Outcome}; {result.Unsupported?.Details}; {string.Join(';', result.Failures)}"));
    [Fact] void should_fail_wrong_values_and_wrong_row_counts() => _report.Results.Where(result => result.Name.StartsWith("Wrong", StringComparison.Ordinal)).Select(result => result.Outcome).ShouldContainOnly(SemanticSpecificationOutcome.Failed, SemanticSpecificationOutcome.Failed);

    static string Source()
    {
        const string model = """
            concept Category : String
            concept Status : Enum
              open
              closed
            module Items
              feature Browsing
                slice StateView Browse
                  command Register
                    id String identifier
                    status Status
                    category Category
                    enabled Bool
                    count Int
                    produces Registered
                      for id
                      status = status
                      category = category
                      enabled = enabled
                      count = count
                  event Registered
                    status Status
                    category Category
                    enabled Bool
                    count Int
                  readmodel Summary
                    id String key
                    status Status
                    category Category
                    enabled Bool
                    count Int
                  projection Summarize => Summary
                    from Registered
                      id = $eventSourceId
                      status = status
                      category = category
                      enabled = enabled
                      count = count
                  query Filtered => Summary[]
                    filter status Status
                    filter category Category optional
                  query ById => Summary[]
                    by id String
                    filter status Status
                  query Scalars => Summary[]
                    filter enabled Bool
                    filter count Int
            """;
        const string events = """
                    given Registered
                      for "a"
                      status = "open"
                      category = "x"
                      enabled = true
                      count = 1
                    given Registered
                      for "b"
                      status = "open"
                      category = "y"
                      enabled = false
                      count = 2
                    given Registered
                      for "c"
                      status = "closed"
                      category = "x"
                      enabled = true
                      count = 1
            """;
        const string seeds = """
                    given readmodel Summary
                      id = "a"
                      status = "open"
                      category = "x"
                      enabled = true
                      count = 1
                    given readmodel Summary
                      id = "b"
                      status = "open"
                      category = "y"
                      enabled = false
                      count = 2
            """;
        static string Case(string name, string fixture, string query, string arguments, params string[] ids) =>
            $"\n      specification {name}\n{fixture}\n        then query {query}\n          arguments\n{arguments}\n" +
            string.Join('\n', ids.Select(id => $"          result\n            id = \"{id}\""));
        const string status = "            status = \"open\"";
        return model +
            Case("OmittedOptional", events, "Filtered", status, "b", "a") +
            Case("SuppliedConcept", events, "Filtered", status + "\n            category = \"x\"", "a") +
            Case("CombinedBy", events, "ById", status + "\n            id = \"a\"", "a") +
            Case("NonMatchingBy", events, "ById", status + "\n            id = \"c\"") +
            Case("NoMatchingRows", events, "Filtered", status + "\n            category = \"missing\"") +
            Case("SeededReversedRows", seeds, "Filtered", status, "b", "a") +
            Case("ScalarFilters", events, "Scalars", "            enabled = true\n            count = 1", "c", "a") +
            Case("WrongRows", events, "Filtered", status, "a") +
            Case("WrongValues", seeds, "Filtered", status, "a", "b") + "\n            status = \"closed\"\n";
    }
}
