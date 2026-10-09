// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_refusing_new_ui_syntax : a_multi_slice_application
{
    [Theory]
    [InlineData("template")]
    [InlineData("component")]
    [InlineData("navigation")]
    [InlineData("guarded")]
    public async Task should_preflight_ui_before_legacy_rendering(string form)
    {
        var location = SourceLocation.Start;
        var binding = new UiBindingSyntax(UiBindingKind.DataContext, "id", location);
        ScreenDirectiveSyntax directive = form switch
        {
            "component" => new ScreenComponentSyntax("core.Table", "items", location) { Properties = [new("value", binding, null, location)] },
            "navigation" => new ScreenNavigateSyntax("Details", null, location) { Parameters = [new("id", binding, location)] },
            _ => new ScreenBehaviorSyntax(new(null, [], [new InteractionBindingSyntax(new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.Click, location), null, [], location) { Otherwise = new([], location) }], location), location)
        };
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.First() with { Screens = [new("Details", null, [new ScreenSectionSyntax("nested", [directive], location)], location)] };
        _application = form == "template" ? _application with { Templates = [new("Assigned", location)] } :
            _application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };
        var error = await Catch.Exception(() => _renderer.Render([_application], _targetDirectory, _output, _error));
        error.ShouldBeOfExactType<RenderingFailed>();
        var failure = ((RenderingFailed)error).Failures.Single();
        if (form == "guarded") failure.ShouldBeOfExactType<UnsupportedGuardedInteraction>();
        else failure.ShouldBeOfExactType<UnsupportedUiSyntax>();
        _scaffolder.WasCalled.ShouldBeFalse();
        _codeOutput.Files.ShouldBeEmpty();
    }
}
