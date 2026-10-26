// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Concurrent;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;

public static class render_compilation
{
    static readonly Lazy<IReadOnlyDictionary<string, render_capture>> Captured = new(() =>
        Names.ToDictionary(
            name => name,
            name => conformance_outcomes.CaptureRenderer(canonical_corpus_vectors.All.Single(vector => vector.Name == name)),
            StringComparer.Ordinal));

    public static IReadOnlyList<string> Names { get; } = ["AcmeAdmissionsCorpus.V29", "EventRoutesCorpus.CompositesV8"];
    public static IReadOnlyDictionary<string, render_capture> Captures => Captured.Value;
    public static artifact_compilations<compilation_result> Compilations { get; } = new();
    public static ConcurrentDictionary<string, Task<compilation_result>> Results { get; } = new(StringComparer.Ordinal);

    public static void AssertAdmitted(this render_capture capture)
    {
        Assert.Equal("supported", capture.Observation.Outcome);
        Assert.NotNull(capture.Request);
        Assert.NotNull(capture.Plan);
        Assert.True(capture.Plan.Success, string.Join(Environment.NewLine, capture.Plan.Diagnostics));
        Assert.NotEmpty(capture.Plan.Artifacts);
    }
}
#endif
