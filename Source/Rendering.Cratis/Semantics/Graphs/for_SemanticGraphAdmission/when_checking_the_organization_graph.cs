// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphAdmission;

public class when_checking_the_organization_graph : Specification
{
    ImmutableArray<ArtifactRenderDiagnostic> _withoutChronicleGraphs;
    ImmutableArray<ArtifactRenderDiagnostic> _withChronicleGraphs;
    ImmutableArray<ArtifactRenderDiagnostic> _lowercaseLabel;
    ImmutableArray<ArtifactRenderDegradation> _omitted;

    void Because()
    {
        var context = an_organization_graph_model.Context();
        _withoutChronicleGraphs = SemanticGraphAdmission.Check(context, chronicleAdmitsGraphs: false);
        _withChronicleGraphs = SemanticGraphAdmission.Check(context, chronicleAdmitsGraphs: true);
        _omitted = SemanticGraphAdmission.Omitted(context);
        _lowercaseLabel = SemanticGraphAdmission.Check(an_organization_graph_model.Context(an_organization_graph_model.Source.Replace("node Person", "node person", StringComparison.Ordinal).Replace("source Person", "source person", StringComparison.Ordinal)), chronicleAdmitsGraphs: true);
    }

    [Fact] void should_refuse_the_graph_once_while_chronicle_has_no_graph_api() => _withoutChronicleGraphs.Select(diagnostic => diagnostic.Code).ShouldEqual([ChronicleGraphSupport.NeedsChronicleGraphs]);
    [Fact] void should_refuse_it_as_an_error() => _withoutChronicleGraphs.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_name_what_is_declared() => _withoutChronicleGraphs.Single().Message.ShouldContain("1 graph(s), 1 graph projection(s), 3 graph specification(s)");
    [Fact] void should_name_the_chronicle_change_that_ships_the_api() => _withoutChronicleGraphs.Single().Message.ShouldContain("Cratis/Chronicle#5680");
    [Fact] void should_admit_every_construct_of_the_sample_once_chronicle_has_it() => _withChronicleGraphs.ShouldBeEmpty();
    [Fact] void should_omit_the_specification_that_seeds_a_graph() => _omitted.Select(degradation => degradation.Diagnostic.Message).Single().ShouldContain("RemovingAPersonDetachesTheirEdges");
    [Fact] void should_report_the_omission_as_a_degradation_warning() => _omitted.Single().Diagnostic.Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    [Fact] void should_code_the_omission_with_the_specification_refusal() => _omitted.Single().Diagnostic.Message.StartsWith($"{SemanticGraphAdmission.Specification}:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_refuse_a_label_that_cannot_be_a_record_name() => _lowercaseLabel.ShouldContain(diagnostic => diagnostic.Code == SemanticGraphAdmission.Unsupported && diagnostic.Message.Contains("PascalCase", StringComparison.Ordinal));
}
#endif
