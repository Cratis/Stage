// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance;

public class when_computing_the_provenance_digest : Specification
{
    // Frozen: a change to this value is a change to the provenance wire contract and needs a new domain string.
    const string Frozen = "aac6f8c01b7d333849713c238199f07f59d245fe0c7cebcef9d3f95437c9ef6a";
    string _digest = null!;

    void Because() => _digest = new ArtifactRenderInputProvenance(
        [new("kit", "1.0.0", new string('1', 64)), new("overlay:acme", "2.0.0", new string('2', 64))],
        [
            new("r1", SemanticImplementationRole.CommandHandler, SemanticAddress.ForApplication(ApplicationIdentity.Create("Projects")), "csharp", "handler.cs", "aa"),
            new("r2", SemanticImplementationRole.QueryPerformer, SemanticAddress.ForApplication(ApplicationIdentity.Create("Projects")), null, null, string.Empty)
        ],
        [
            new(ArtifactRenderComponentKind.Kit, "acme", "1.0.0", new string('3', 64)),
            new(ArtifactRenderComponentKind.TemplateEngine, "scriban", "7.5.3", null)
        ]).Digest;

    [Fact] void should_match_the_frozen_vector() => _digest.ShouldEqual(Frozen);
}
