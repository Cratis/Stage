// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

/// <summary>
/// Reactions, absence assertions, generated values and responses cannot be executed by Stage; a run that would
/// silently skip them is reported as unsupported, never as passing.
/// </summary>
public class when_refusing_later_version_constructs : Specification
{
    readonly List<(string Corpus, SemanticSpecificationRunRecord Result)> _results = [];

    async Task Because()
    {
        foreach (var corpus in new[] { ReadModelAbsenceCorpus.V5, ReactionsCorpus.V6, RegisterProjectCorpus.V7 })
        {
            var model = SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan());
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            var specifications = model.Application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
                .SelectMany(slice => slice.Specifications).Where(Uses(plan)).Select(specification => specification.Id).ToArray();
            Assert.NotEmpty(specifications);
            var report = await new SemanticSpecificationExecutor().Run(plan, new([.. specifications]), new());
            _results.AddRange(report.Results.Select(result => (corpus.Name, result)));
        }
    }

    [Fact] void should_report_every_affected_specification_as_unsupported() => Assert.True(
        _results.TrueForAll(_ => _.Result.Outcome == SemanticSpecificationOutcome.Unsupported),
        string.Join(Environment.NewLine, _results.Select(_ => $"{_.Corpus} {_.Result.Name}: {_.Result.Outcome} {string.Join(',', _.Result.Failures)}")));

    [Fact] void should_cover_each_version() => _results.Select(_ => _.Corpus).Distinct().Count().ShouldEqual(3);

    static Func<SemanticSpecification, bool> Uses(SemanticExecutionPlan plan) => specification =>
        !specification.ThenAbsentReadModels.IsDefaultOrEmpty || specification.ThenReturns is not null ||
        specification.When is { GeneratedValues.IsDefaultOrEmpty: false } ||
        (specification.When is { } when && plan.Commands.TryGetValue(when.Command, out var command) && (command.Response is not null || command.Properties.Any(property => property.IsGenerated))) ||
        plan.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices).Any(slice => !slice.Reactions.IsDefaultOrEmpty);

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(AllSlices));
}
#endif
