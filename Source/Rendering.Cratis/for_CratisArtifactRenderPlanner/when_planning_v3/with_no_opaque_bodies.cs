// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_v3.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_v3;

public class with_no_opaque_bodies : Specification
{
    bool _equivalent;

    void Because()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource);
        var previous = invoice_model.Plan(invoice_model.Compile(source));
        var current = a_v3_invoice.Plan(a_v3_invoice.Model());
        _equivalent = previous.Success && current.Success &&
            previous.Artifacts.Select(artifact => (artifact.RelativePath, Bytes: Convert.ToHexString(artifact.Bytes.AsSpan())))
                .SequenceEqual(current.Artifacts.Select(artifact => (artifact.RelativePath, Bytes: Convert.ToHexString(artifact.Bytes.AsSpan()))));
    }

    [Fact] void should_render_the_same_artifact_bytes_as_v2() => _equivalent.ShouldBeTrue();
}
