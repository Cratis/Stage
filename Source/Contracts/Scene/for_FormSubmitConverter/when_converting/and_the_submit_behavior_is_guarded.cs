// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_FormSubmitConverter.when_converting;

public class and_the_submit_behavior_is_guarded : Specification
{
    Cratis.Scene.Model.Forms.Form _form = null!;
    ConversionDiagnostics _diagnostics = null!;
    Exception? _exception;

    void Because()
    {
        var location = new SourceLocation(4, 3, "forms.play");
        var action = new NavigateActionSyntax("Details", location);
        var behavior = new BehaviorSyntax(null, [], [new InteractionBindingSyntax(new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.Submit, location), "item.ready", [action], location)], location);
        var form = new FormSyntax("Register", "Register", null, [], null, location) { Behaviors = [behavior] };
        _diagnostics = new ConversionDiagnostics();
        var scope = new BehaviorScope(new Dictionary<string, BehaviorSyntax>(), new List<RenderFinding>())
        {
            Diagnostics = _diagnostics,
            CommandResponses = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
            {
                ["Register"] = new Dictionary<string, string>(StringComparer.Ordinal) { ["id"] = string.Empty },
                ["Confirm"] = new Dictionary<string, string>(StringComparer.Ordinal) { ["receipt"] = "receipt" }
            }
        };
        _exception = Catch.Exception(() => _form = FormSubmitConverter.Convert(form, scope, "Overview.form.Register"));
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_report_one_finding() => _diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-FORM-002");
    [Fact] void should_keep_the_guarded_behavior() => _form.Behaviors.Single().Bindings.Single().Actions.Count.ShouldEqual(1);
    [Fact] void should_not_move_it_into_the_continuation() => _form.OnSubmit.ShouldBeEmpty();
}
