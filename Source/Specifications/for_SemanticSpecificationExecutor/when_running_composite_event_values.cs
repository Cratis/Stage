// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_composite_event_values : Specification
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun[] _reference = [];

    async Task Because()
    {
        var plan = Compile();
        _reference = [.. plan.Specifications.Keys.Select(id => new SemanticSpecificationRunner().Run(plan, id))];
        _report = await new SemanticSpecificationExecutor().Run(plan, new([]), new());
    }

    [Fact] void should_cover_given_produced_appended_and_invoked_values() => _report.Results.Count.ShouldEqual(4);
    [Fact] void should_agree_with_the_reference() => Assert.All(_reference, result => Assert.True(result.Passed, string.Join(';', result.Failures)));
    [Fact] void should_execute_every_specification() => Assert.All(_report.Results, result => Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Name}: {result.Outcome} {result.Unsupported?.Details} {string.Join(';', result.Failures)}"));
    [Fact] void should_report_composite_facts_as_member_name_keyed_json() => Assert.All(_report.Results.SelectMany(result => result.Trace!.Facts), fact => Assert.Equal("nested", JsonSerializer.Deserialize<JsonElement>(Assert.Single(fact.Values).Value).GetProperty("label").GetString()));

    public static SemanticExecutionPlan Compile()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("CompositeFacts"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("facts"), "facts", "Facts.play", Source);
        var compilation = new SemanticModelCompiler().Compile("CompositeFacts", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        return SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    public const string Source = """
        type Child
          count Int
        type Details
          label String
          child Child
        module Facts
          feature Recording
            slice StateChange Record
              command RecordDetails
                id String identifier
                details Details
                produces DetailsRecorded
                  for id
                  details = details
              event DetailsRecorded
                details Details
              specification ProducedComposite
                when RecordDetails
                  id = "one"
                  details = {"label":"nested","child":{"count":7}}
                then DetailsRecorded
                  details = {"child":{"count":7},"label":"nested"}
              specification GivenComposite
                given DetailsRecorded
                  for "before"
                  details = {"label":"nested","child":{"count":6}}
                when RecordDetails
                  id = "one"
                  details = {"label":"nested","child":{"count":7}}
                then DetailsRecorded
                  details = {"child":{"count":7},"label":"nested"}
              specification AppendedComposite
                when append DetailsRecorded
                  for "one"
                  details = {"label":"nested","child":{"count":7}}
                then DetailsRecorded
                  details = {"child":{"count":7},"label":"nested"}
            slice Automation Forward
              event DetailsRequested
                details Details
              reaction ForwardDetails
                when DetailsRequested
                  invokes RecordDetails
                    id = "one"
                    details = details
              specification InvokedComposite
                when append DetailsRequested
                  for "one"
                  details = {"label":"nested","child":{"count":7}}
                then DetailsRecorded
                  details = {"child":{"count":7},"label":"nested"}
        """;
}
