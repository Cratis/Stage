// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_publishing_required_values_from_different_sources : given.a_multi_source_publication
{
    void Because() => _plan = Plan(_model);

    [Fact]
    void should_never_admit_uncompilable_publication_output()
    {
        if (!_plan.Success)
        {
            _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-032" && diagnostic.Message.Contains("OrderPublisher", StringComparison.Ordinal));
            return;
        }
        var sources = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
        RenderedOutput.Errors(sources).ShouldBeEmpty();
    }

    [Fact] void should_refuse_until_required_fields_can_be_rebuilt_from_private_history() => given.rendering_refusals.Rejected(_plan, "STAGE-ESM-032");
    [Fact] void should_emit_no_partial_publication() => given.rendering_refusals.OmittedParts(_plan);
}
#endif
