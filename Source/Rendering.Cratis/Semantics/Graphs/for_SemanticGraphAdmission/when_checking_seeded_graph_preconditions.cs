// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphAdmission;

public class when_checking_seeded_graph_preconditions : Specification
{
    ImmutableArray<ArtifactRenderDiagnostic> _seeded;
    ImmutableArray<ArtifactRenderDiagnostic> _seededOnly;
    ImmutableArray<ArtifactRenderDiagnostic> _absent;

    void Because()
    {
        var specification = a_seeded_graph_model.Request().Model.Application.GraphSpecifications.Single();
        var step = specification.GivenGraphs.Single();
        var element = step.Elements.Single();
        _seeded = Check(specification);
        _seededOnly = Check(specification with { WhenAppended = null });
        _absent = Check(specification with { GivenGraphs = [step with { Elements = [element with { Absent = true, Values = [] }] }] });
    }

    [Fact] void should_admit_seeded_graphs_with_events() => _seeded.ShouldBeEmpty();
    [Fact] void should_admit_seeded_graphs_without_events() => _seededOnly.ShouldBeEmpty();
    [Fact] void should_refuse_an_absent_seed() => _absent.ShouldContain(diagnostic => diagnostic.Code == SemanticGraphAdmission.Specification && diagnostic.Message.Contains("absent", StringComparison.Ordinal));

    static ImmutableArray<ArtifactRenderDiagnostic> Check(SemanticGraphSpecification specification)
    {
        var request = a_seeded_graph_model.Request();
        var model = ExecutableSemanticModel.Create(request.Model.LanguageVersion, request.Model.SemanticVersion, request.Model.Application with { GraphSpecifications = [specification] });
        return SemanticGraphAdmission.Check(new(request with { Model = model }, new("Organization", "Organization")), chronicleAdmitsGraphs: true);
    }
}
#endif
