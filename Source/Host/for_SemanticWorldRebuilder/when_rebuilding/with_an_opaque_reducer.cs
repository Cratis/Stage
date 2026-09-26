// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_an_opaque_reducer : a_rebuildable_world
{
    Exception? _error;

    void Because()
    {
        var source = Source.Replace(
            "projection ProjectSummaryProjection => ProjectSummary\n        from ProjectRegistered key projectId\n          name = name",
            "reducer BalanceReducer => ProjectSummary\n        on ProjectRegistered\n          file Reducers/Deposited.cs",
            StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("reducer"), "reducer", "Reducer.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.True(plan.Success, string.Join("; ", plan.Issues));
        _error = Catch.Exception(() => SemanticWorldRebuilder.Create(plan.Plan!, [_event], 0));
    }

    [Fact] void should_refuse_the_unsupported_projection() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_name_the_reducer() => _error!.Message.ShouldContain("BalanceReducer");
}
