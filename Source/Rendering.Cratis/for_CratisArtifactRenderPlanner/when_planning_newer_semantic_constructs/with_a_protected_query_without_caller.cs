// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

/// <summary>
/// A protected query may not fall back to direct invocation when the spec has no fixture principal. Since Screenplay 4.180 the language
/// itself refuses the missing <c>given caller</c> (PLAY0389) before a model exists to plan, so the rule lives in the compiler and Stage never sees it.
/// </summary>
public class with_a_protected_query_without_caller : Specification
{
    Exception? _refusal;

    void Because()
    {
        var source = "policy Clerks\n  require authenticated\n" + when_rendering_scoped_projections.ScopedSource
            .Replace("query ProjectById => ProjectSummary?\n", "query ProjectById => ProjectSummary?\n                authorize Clerks\n", StringComparison.Ordinal);
        _refusal = Catch.Exception(() => invoice_model.Plan(invoice_model.Compile(source)));
    }

    [Fact] void should_refuse_the_model_before_it_can_be_planned() => _refusal.ShouldNotBeNull();
    [Fact] void should_refuse_it_with_the_language_diagnostic() => _refusal!.Message.ShouldContain("PLAY0389");
    [Fact] void should_name_the_missing_caller_fixture() => _refusal!.Message.ShouldContain("given caller");
}
