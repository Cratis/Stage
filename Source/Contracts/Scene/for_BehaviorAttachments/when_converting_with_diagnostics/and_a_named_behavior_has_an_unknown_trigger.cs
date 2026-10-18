// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using SceneModel = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorAttachments.when_converting_with_diagnostics;

public class and_a_named_behavior_has_an_unknown_trigger : Specification
{
    ConversionDiagnostics _diagnostics = null!;
    BehaviorScope _scope = null!;
    IReadOnlyList<SceneModel.Behavior> _result = null!;
    readonly SourceLocation _location = new(4, 3, "behaviors.play");

    void Establish()
    {
        _diagnostics = new();
        var behavior = new BehaviorSyntax("Refresh", [], [new InteractionBindingSyntax(new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.Unknown, _location), null, [], _location)], _location);
        _scope = new(BehaviorAttachments.Declared([behavior]), []) { Diagnostics = _diagnostics };
    }

    void Because() => _result = _scope.Resolve([], [new UsesBehaviorSyntax("Refresh", [], _location)], "Projects.List");

    [Fact] void should_retain_the_named_behavior() => _result.Single().Name.ShouldEqual("Refresh");
    [Fact] void should_omit_the_unsupported_binding() => _result.Single().Bindings.ShouldBeEmpty();
    [Fact] void should_report_the_trigger() => _diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-BEHAVIOR-001");
    [Fact] void should_report_the_attachment_path() => _diagnostics.All.Single().Path.ShouldEqual("Projects.List");
    [Fact] void should_report_the_declaration_location() => _diagnostics.All.Single().Location.ShouldEqual(_location);
}
