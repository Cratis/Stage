// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_seeding_given_read_models;

public class with_unrepresentable_projected_state : Specification
{
    readonly List<SemanticUnsupportedCapability?> _refusals = [];

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("SeedAdmission"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", with_keyed_state.Source);
        var compilation = new SemanticModelCompiler().Compile("SeedAdmission", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var original = plan.Specifications.Values.Single(specification => specification.Name == "FoldingAnAppend");
        var seed = original.GivenReadModels.Single();
        var count = seed.Values.Single(value => value.Value is SemanticNumberValue);
        var identifier = plan.ReadModels[seed.ReadModel].Properties.Single(property => property.IsIdentifier);
        var seeds = new[]
        {
            seed with { Values = [.. seed.Values.Where(value => value.TargetProperty != count.TargetProperty)] },
            seed with { Values = [.. seed.Values.Select(value => value.TargetProperty == count.TargetProperty ? value with { Value = SemanticValue.Null } : value)] },
            seed with { Values = [.. seed.Values.Select(value => value.TargetProperty == identifier.Id ? value with { Value = SemanticValue.Text("other") } : value)] }
        };
        foreach (var candidateSeed in seeds)
        {
            // Exercise admission directly: the compiler also rejects incomplete source fixtures.
            _refusals.Add(SemanticRunAdmission.Check(plan, original with { GivenReadModels = [candidateSeed] }));
        }
    }

    [Fact] void should_check_all_three_lossy_seeds() => _refusals.Count.ShouldEqual(3);
    [Fact] void should_refuse_partial_null_or_mismatched_seeds() => _refusals.ShouldEachConformTo(refusal => refusal is not null);
    [Fact] void should_name_the_given_read_model_capability() => _refusals.ShouldEachConformTo(refusal => refusal?.Capability == StageExecutionCapability.GivenReadModel);
}
