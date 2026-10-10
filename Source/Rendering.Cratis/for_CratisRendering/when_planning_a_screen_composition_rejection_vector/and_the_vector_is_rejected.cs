// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_a_screen_composition_rejection_vector;

/// <summary>
/// Every rejection vector of the canonical screen composition corpus, planned through Stage's source entry point.
/// </summary>
/// <remarks>
/// The corpus states the exact diagnostics each vector produces and that it produces no artifacts. Stage has to
/// agree on both: a vector that plans anything at all would mean Stage renders a composition the language refuses.
/// </remarks>
public class and_the_vector_is_rejected : Specification
{
    public static TheoryData<string> Vectors => [.. ScreenCompositionCorpus.V1.RejectionVectors.Select(vector => vector.Name)];

    [Theory]
    [MemberData(nameof(Vectors))]
    public async Task should_report_exactly_the_corpus_diagnostics(string name)
    {
        // Compared as an exact multiset: the corpus lists diagnostics in source order, which is the order the syntax
        // compiler reports them in, while the semantic compiler Stage plans through reports a re-exposure cycle
        // from its other end. Stage passes the compiler's order through rather than re-sorting it.
        var (vector, result) = await Plan(name);
        Errors(result).Order(StringComparer.Ordinal).ShouldEqual([.. vector.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").Order(StringComparer.Ordinal)]);
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public async Task should_plan_no_artifacts(string name)
    {
        var (vector, result) = await Plan(name);
        result.Artifacts.Select(artifact => artifact.RelativePath).ShouldEqual([.. vector.ArtifactPaths]);
        result.Artifacts.ShouldBeEmpty();
    }

    [Fact] void should_cover_every_corpus_rejection_vector() => Vectors.Count.ShouldEqual(6);

    static string[] Errors(CratisPlanResult result) =>
        [.. result.Diagnostics
            .Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && diagnostic.Code.StartsWith("PLAY", StringComparison.Ordinal))
            .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")];

    static async Task<(CanonicalCorpusRejectionVector Vector, CratisPlanResult Result)> Plan(string name)
    {
        var vector = ScreenCompositionCorpus.V1.RejectionVectors.Single(candidate => candidate.Name == name);
        var root = Path.Combine(Path.GetTempPath(), $"stage-composition-rejection-{Guid.NewGuid():N}");
        try
        {
            foreach (var document in vector.SourceForm.Documents)
            {
                var path = Path.Combine(root, document.DisplayPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllBytesAsync(path, [.. document.Bytes]);
            }

            var result = await CratisRendering.PlanFrom(
                new PlaySources(root, []),
                new PlanSelection([PlanSelectionEntry.Module(vector.ApplicationName)]),
                new CratisPlanOptions(vector.ApplicationName, vector.ApplicationName, vector.ApplicationName));

            return (vector, result);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
