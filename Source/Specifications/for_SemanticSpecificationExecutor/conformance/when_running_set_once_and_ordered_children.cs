// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.conformance;

// Screenplay ESM v43, run by both engines: a set-once property keeps the first value it receives, and a children collection places a new child where its
// ordering says (last by default, first, or sorted by a child property).
public class when_running_set_once_and_ordered_children : Specification
{
    const string Source = """
        type Entry
          code String
        type Tag
          name String
        module Library
          feature Records
            slice StateView Records
              event RecordOpened
                owner String
              event EntryAdded
                code String
              event TagAdded
                name String
              command OpenRecord
                record String identifier
                owner String
                produces RecordOpened
                  for record
                  owner = owner
              command AddEntry
                record String identifier
                code String
                produces EntryAdded
                  for record
                  code = code
              command AddTag
                record String identifier
                name String
                produces TagAdded
                  for record
                  name = name
              readmodel Record
                id String key
                owner String
                entries Entry[]
                tags Tag[]
              projection Build => Record
                no automap
                from RecordOpened
                  set once owner = owner
                children entries identified by code
                  insert first
                  from EntryAdded key code
                    parent $eventSourceId
                    code = code
                children tags identified by name
                  sort by name descending
                  from TagAdded key name
                    parent $eventSourceId
                    name = name
              specification TheFirstOwnerIsKept
                given RecordOpened
                  for "r1"
                  owner = "first"
                given RecordOpened
                  for "r1"
                  owner = "second"
                then readmodel Record
                  id = "r1"
                  owner = "first"
                  entries none
                  tags none
              specification ChildrenAreOrdered
                given RecordOpened
                  for "r1"
                  owner = "o"
                given EntryAdded
                  for "r1"
                  code = "a"
                given EntryAdded
                  for "r1"
                  code = "b"
                given EntryAdded
                  for "r1"
                  code = "c"
                given TagAdded
                  for "r1"
                  name = "m"
                given TagAdded
                  for "r1"
                  name = "z"
                given TagAdded
                  for "r1"
                  name = "a"
                then readmodel Record
                  id = "r1"
                  owner = "o"
                  entries
                    item
                      code = "c"
                    item
                      code = "b"
                    item
                      code = "a"
                  tags
                    item
                      name = "z"
                    item
                      name = "m"
                    item
                      name = "a"
        """;

    readonly List<string> _differences = [];
    int _executed;

    [Fact] void should_compile_the_model() => _differences.ShouldNotContain(difference => difference.StartsWith("compile"));
    [Fact] void should_run_every_specification() => _executed.ShouldEqual(2);
    [Fact] void should_match_the_reference_for_every_specification() => Assert.True(_differences.Count == 0, string.Join(Environment.NewLine, _differences));

    protected async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("records"), "records", "Records.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
        if (!compilation.Success)
        {
            _differences.Add("compile: " + string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.ToString())));
            return;
        }

        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        foreach (var specification in plan.Specifications.Values)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            var result = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
            _executed++;
            if (!reference.Passed || result.Outcome != SemanticSpecificationOutcome.Passed)
            {
                _differences.Add($"{specification.Name}: reference {reference.Execution.Kind}/{reference.Passed} {string.Join(';', reference.Failures)}; Stage {result.Outcome} {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
            }
        }
    }
}
