// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_seeding_given_read_models;

public class with_keyed_state : Specification
{
    internal const string Source = """
        command reads protected
        module Projects
          feature Registry
            slice StateChange Naming
              event Established
                name String
                count Int
              event Renamed
                name String
              event Deleted
              command Establish
                id String identifier
                name String
                count Int
                produces Established
                  for id
                  name = name
                  count = count
              command Rename
                id String identifier
                expected String
                name String
                reads Summary by id
                validate
                  require Summary.name == expected
                    message "Unexpected name."
                produces Renamed
                  for id
                  name = name
              command Delete
                id String identifier
                produces Deleted
                  for id
              readmodel Summary
                id String key
                name String
                count Int
              projection Summarize => Summary
                from Established
                  id = $eventSourceId
                  name = name
                  count = count
                from Renamed
                  name = name
                remove with Deleted
              query SummaryById => Summary optional
                by id String
              specification ReplacingHistoryBeforeReading
                given Established
                  for "one"
                  name = "history"
                  count = 1
                given Established
                  for "two"
                  name = "untouched"
                  count = 2
                given readmodel Summary
                  id = "one"
                  name = "seed"
                  count = 42
                given readmodel Summary
                  id = "three"
                  name = "seed-only"
                  count = 3
                when Rename
                  id = "one"
                  expected = "seed"
                  name = "action"
                then Renamed
                  for "one"
                  name = "action"
                then readmodel Summary
                  id = "one"
                  name = "action"
                  count = 42
                then readmodel Summary
                  id = "two"
                  name = "untouched"
                  count = 2
                then readmodel Summary
                  id = "three"
                  name = "seed-only"
                  count = 3
              specification QueryingWithoutEvents
                given readmodel Summary
                  id = "one"
                  name = "seed"
                  count = 42
                then query SummaryById
                  arguments
                    id = "one"
                  result
                    id = "one"
                    name = "seed"
                    count = 42
              specification FoldingAnAppend
                given readmodel Summary
                  id = "one"
                  name = "seed"
                  count = 42
                when append Renamed
                  for "one"
                  name = "appended"
                then readmodel Summary
                  id = "one"
                  name = "appended"
                  count = 42
              specification RemovingSeededState
                given readmodel Summary
                  id = "one"
                  name = "seed"
                  count = 42
                when Delete
                  id = "one"
                then Deleted
                  for "one"
                then no readmodel Summary for "one"
              specification NotLeakingSeeds
                then query SummaryById
                  arguments
                    id = "one"
        """;

    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRunReport _repeated = null!;
    SemanticSpecificationRunReport _wrong = null!;

    async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("SeededState"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("SeededState", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var executor = new SemanticSpecificationExecutor();
        _report = await executor.Run(_plan, new([]), new());
        _repeated = await executor.Run(_plan, new([]), new());
        var module = _plan.Model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var original = slice.Specifications.Single(specification => specification.Name == "FoldingAnAppend");
        var expected = original.ThenReadModels.Single();
        var wrong = original with { ThenReadModels = [expected with { Values = [.. expected.Values.Select(value => value.Value is SemanticNumberValue ? value with { Value = SemanticValue.Number(999) } : value)] }] };
        var application = _plan.Model.Application with { Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [wrong] }] }] }] };
        var candidate = ExecutableSemanticModel.Create(_plan.Model.LanguageVersion, _plan.Model.SemanticVersion, application);
        _wrong = await executor.Run(SemanticExecutionPlan.Compile(candidate).Plan!, new([]), new());
    }

    [Fact] void should_execute_every_seeded_case() => Assert.All(_report.Results, result => Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Name}: {result.Outcome}; {result.Unsupported?.Details}; {string.Join(';', result.Failures)}"));
    [Fact] void should_match_the_reference() => _plan.Specifications.Values.ShouldEachConformTo(specification => new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed);
    [Fact] void should_cover_five_distinct_worlds() => _report.Results.Count.ShouldEqual(5);
    [Fact] void should_repeat_without_leaking_world_state() => _repeated.Results.Select(result => result.Outcome).ShouldEqual(_report.Results.Select(result => result.Outcome));
    [Fact] void should_detect_an_incorrect_seeded_value_expectation() => _wrong.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
}
