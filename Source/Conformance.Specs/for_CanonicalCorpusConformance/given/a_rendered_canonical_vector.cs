// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

public class a_rendered_canonical_vector : Specification
{
    protected ArtifactRenderPlan _plan = null!;
    protected const string Vector = "AcmeAdmissionsCorpus.V29";
    protected const string Probe = "\n#if DEBUG\nclass Stage824Probe { MissingType _f; }\n#endif";

    void Establish() => _plan = conformance_outcomes.RenderPlan(canonical_corpus_vectors.All.Single(vector => vector.Name == Vector))!;

    protected PlannedArtifact[] Corrupt(string path) =>
        [.. _plan.Artifacts.Select(file => file.RelativePath == path
            ? PlannedArtifact.CreateText(path, Encoding.UTF8.GetString(file.Bytes.AsSpan()) + Probe)
            : file)];
}
#endif
