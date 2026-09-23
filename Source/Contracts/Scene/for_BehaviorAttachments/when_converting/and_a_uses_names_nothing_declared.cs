// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using SceneModel = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorAttachments.when_converting;

/// <summary>
/// Reported and dropped, not substituted with an empty behavior. The compiler reports it too; this is the
/// second line of defence for a model assembled from parts that did not compile together.
/// </summary>
public class and_a_uses_names_nothing_declared : Specification
{
    IReadOnlyList<SceneModel.Behavior> _result = null!;
    List<RenderFinding> _findings = null!;

    void Establish() => _findings = [];

    void Because() => _result = BehaviorAttachments.Convert(
        [],
        [new UsesBehaviorSyntax("NoSuchBehavior", [], SourceLocation.Start)],
        BehaviorAttachments.Declared([]),
        "InvoiceList",
        _findings);

    [Fact] void should_attach_nothing() => _result.ShouldBeEmpty();
    [Fact] void should_report_it() => _findings.Count.ShouldEqual(1);
    [Fact] void should_report_it_as_a_missing_behavior() => _findings[0].Kind.ShouldEqual(RenderFindingKind.BehaviorNotFound);
    [Fact] void should_report_what_was_attached_to() => _findings[0].Subject.ShouldEqual("InvoiceList");
    [Fact] void should_name_the_behavior_in_the_message() => _findings[0].Message.ShouldContain("NoSuchBehavior");
}
