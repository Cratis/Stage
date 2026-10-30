// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_property_keyed_root_joins : Specification
{
    const string SourceTemplate = """
        concept ForeignKey : {{primitive}}
        module Work
          feature Work
            slice StateChange Recording
              command Open
                sourceId String identifier
                rootId String
                foreignKey ForeignKey
                label String
                produces Opened
                  for sourceId
                  rootId = rootId
                  foreignKey = foreignKey
                  label = label
              command Rename
                sourceId String identifier
                correlationId ForeignKey
                label String
                produces Renamed
                  for sourceId
                  correlationId = correlationId
                  label = label
              event Opened
                rootId String
                foreignKey ForeignKey
                label String
              event Renamed
                correlationId ForeignKey
                label String
              specification RootsFirst
                given Opened
                  for "origin-a"
                  rootId = "a"
                  foreignKey = "{{target}}"
                  label = "before"
                given Opened
                  for "origin-b"
                  rootId = "b"
                  foreignKey = "{{target}}"
                  label = "before"
                given Opened
                  for "origin-decoy"
                  rootId = "decoy"
                  foreignKey = "{{decoy}}"
                  label = "before"
                when append Renamed
                  for "{{decoy}}"
                  correlationId = "{{joinKey}}"
                  label = "after"
                then readmodel Root
                  id = "a"
                  label = "{{firstExpected}}"
                then readmodel Root
                  id = "b"
                  label = "{{expected}}"
                then readmodel Root
                  id = "decoy"
                  label = "before"
                then query RootById
                  arguments
                    id = "{{decoy}}"
            slice StateView Listing
              readmodel Root
                id String key
                foreignKey ForeignKey
                label String
              query RootById => Root optional
                by id String
              projection Build => Root
                no automap
                from Opened key rootId
                  foreignKey = foreignKey
                  label = label
                join label on foreignKey
                  with Renamed
                    label = label
        """;

    [Theory]
    [InlineData("String", false, false, false)]
    [InlineData("Uuid", false, false, false)]
    [InlineData("String", true, false, false)]
    [InlineData("Uuid", true, false, false)]
    [InlineData("String", false, true, false)]
    [InlineData("Uuid", false, true, false)]
    [InlineData("String", false, false, true)]
    [InlineData("Uuid", false, false, true)]
    public async Task should_match_the_reference_without_creating_or_updating_source_matched_decoys(string primitive, bool unmatched, bool wrong, bool givenJoin)
    {
        var plan = Compile(primitive, unmatched, wrong, givenJoin);
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single()).ShouldBeNull();
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        reference.Passed.ShouldEqual(!wrong);
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == (wrong ? SemanticSpecificationOutcome.Failed : SemanticSpecificationOutcome.Passed),
            $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        result.Failures.ShouldContainOnly(reference.Failures);
    }

    static SemanticExecutionPlan Compile(string primitive, bool unmatched, bool wrong, bool givenJoin)
    {
        var target = primitive == "Uuid" ? "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01" : "target-key";
        var decoy = primitive == "Uuid" ? "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f02" : "decoy-key";
        var missing = primitive == "Uuid" ? "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f03" : "missing-key";
        var expected = unmatched ? "before" : "after";
        var source = SourceTemplate.Replace("{{primitive}}", primitive, StringComparison.Ordinal)
            .Replace("{{target}}", target, StringComparison.Ordinal).Replace("{{decoy}}", decoy, StringComparison.Ordinal)
            .Replace("{{joinKey}}", unmatched ? missing : target, StringComparison.Ordinal)
            .Replace("{{firstExpected}}", wrong ? "wrong" : expected, StringComparison.Ordinal)
            .Replace("{{expected}}", expected, StringComparison.Ordinal);
        if (givenJoin)
        {
            source = WithGivenJoin(source, target);
        }
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RootJoin"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Join.play", source);
        var compilation = new SemanticModelCompiler().Compile("RootJoin", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var key = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Events).Single(@event => @event.Name == "Renamed").Properties.Single(property => property.Name == "correlationId").Id;
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Projections = [.. slice.Projections.Select(projection => projection.Scope is { Joins.Length: > 0 } scope
                            ? projection with { Scope = scope with { Joins = [.. scope.Joins.Select(join => join with { Key = new SemanticProjectionValueKey(new SemanticProjectionEventProperty([key])) })] } }
                            : projection)]
                    })]
                })]
            })]
        };
        var execution = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application));
        Assert.NotNull(execution.Plan);
        return execution.Plan;
    }

    static string WithGivenJoin(string source, string target) => source.Replace("when append Renamed", "given Renamed", StringComparison.Ordinal)
        .Replace(
            "        then readmodel Root\n          id = \"a\"",
            $$"""
                    when append Opened
                      for "origin-a"
                      rootId = "a"
                      foreignKey = "{{target}}"
                      label = "after"
                    then readmodel Root
                      id = "a"
            """,
            StringComparison.Ordinal);
}
