// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_malformed_stored_source : a_rebuildable_world
{
    Exception? _error;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(
            catalog.ResolveDocument("text-source"),
            "text-source",
            "TextSource.play",
            Source.Replace("concept ProjectId : Uuid", "concept ProjectId : String", StringComparison.Ordinal));
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.True(plan.Success, string.Join("; ", plan.Issues));
        _event.Context.EventSourceId = " ";
        _error = Catch.Exception(() => SemanticWorldRebuilder.Create(plan.Plan!, [_event], 0));
    }

    [Fact] void should_refuse_the_evaluators_rejection() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_explain_the_malformed_occurrence() => _error!.Message.ShouldContain("Fact occurrence metadata is malformed");
}
