// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_projection_chronicle_cannot_mirror : a_rebuildable_world
{
    SemanticWorld _world = null!;

    void Because()
    {
        var source = Source.Replace("name ProjectName\n      query ProjectById", "name ProjectName?\n      query ProjectById", StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("unmirrored"), "unmirrored", "Unmirrored.play", source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
        Assert.True(plan.ReadModels.Values.Single().Properties.Single(property => property.Name == "name").Type.IsOptional, source);
        _world = SemanticWorldRebuilder.Create(plan, [_event], 0);
    }

    [Fact] void should_project_from_the_log_without_a_chronicle_mirror() => _world.ReadModels.Single().Key.ShouldEqual(_commandValues[0].Value);
}
