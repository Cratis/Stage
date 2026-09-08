// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_a_primitive_scope_with_unrelated_concepts : Specification
{
    ArtifactRenderPlan _plan = null!;
    IReadOnlyList<string> _errors = null!;
    IReadOnlyList<string> _warnings = null!;
    RenderedFile[] _sources = null!;

    void Because()
    {
        var model = invoice_model.Compile("concept Unrelated : String\n" + invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource));
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        _plan = invoice_model.Plan(model, new(ArtifactRenderScopeKind.Slice, slice.Id));
        _sources = [.. _plan.Artifacts.Select(_ => new RenderedFile(_.RelativePath, Encoding.UTF8.GetString(_.Bytes.AsSpan())))];
        _errors = RenderedOutput.Errors(_sources);
        _warnings = RenderedOutput.Warnings(_sources);
    }

    [Fact] void should_admit_the_slice() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_only_the_command_and_its_two_specifications() => _sources.Length.ShouldEqual(3);
    [Fact] void should_not_import_an_unrelated_global_concept_namespace() => _sources.All(_ => !_.Content.Contains("using Invoices.Common;", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compile_the_slice_without_any_common_files() => string.Join(Environment.NewLine, _errors).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, _warnings).ShouldEqual(string.Empty);
}
