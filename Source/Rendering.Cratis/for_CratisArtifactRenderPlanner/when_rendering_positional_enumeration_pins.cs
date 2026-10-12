// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_positional_enumeration_pins : Specification
{
    byte[] _originalModel = null!;
    byte[] _pinnedModel = null!;
    byte[] _original = null!;
    byte[] _pinned = null!;
    string _zero = null!;

    void Because()
    {
        var original = an_enumeration_model.Compile();
        var pinned = an_enumeration_model.Compile(an_enumeration_model.Source.Replace("  draft\n  issued", "  draft = -0\n  issued = 1", StringComparison.Ordinal));
        var numbered = an_enumeration_model.Compile(an_enumeration_model.Source.Replace("  draft\n  issued", "  draft = -0\n  issued = 90", StringComparison.Ordinal));
        _originalModel = SemanticModelSerializer.Serialize(original);
        _pinnedModel = SemanticModelSerializer.Serialize(pinned);
        _original = [.. invoice_model.Plan(original).Artifacts.Single(artifact => artifact.RelativePath == "Common/Status.cs").Bytes];
        _pinned = [.. invoice_model.Plan(pinned).Artifacts.Single(artifact => artifact.RelativePath == "Common/Status.cs").Bytes];
        _zero = Encoding.UTF8.GetString(invoice_model.Plan(numbered).Artifacts.Single(artifact => artifact.RelativePath == "Common/Status.cs").Bytes.AsSpan());
    }

    [Fact] void should_keep_existing_semantic_bytes() => _pinnedModel.ShouldEqual(_originalModel);
    [Fact] void should_keep_existing_generated_bytes() => _pinned.ShouldEqual(_original);
    [Fact] void should_canonicalize_negative_zero() => _zero.ShouldContain("Draft = 0,");
}
