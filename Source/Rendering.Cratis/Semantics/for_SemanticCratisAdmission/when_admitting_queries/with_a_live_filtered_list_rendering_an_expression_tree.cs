// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_queries;

public class with_a_live_filtered_list_rendering_an_expression_tree : Specification
{
    ArtifactRenderPlan _plan = null!;
    RenderedFile[] _files = [];

    void Because()
    {
        _plan = invoice_model.Plan(invoice_model.Compile(a_query_filter_model.Source));
        _files = [.. _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
    }

    [Fact] void should_admit_the_filtered_live_list() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_every_expression_tree() => RenderedOutput.Errors(_files).ShouldBeEmpty();
}
