// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_refusing_syntax_public_events : a_multi_slice_application
{
    [Theory]
    [InlineData("public", "STAGE-ESM-031")]
    [InlineData("foreign", "STAGE-ESM-032")]
    [InlineData("direction", "STAGE-ESM-024")]
    public async Task should_refuse_every_entrypoint_before_any_output(string form, string code)
    {
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var selected = feature.Slices.First();
        selected = form == "direction"
            ? selected with { Type = global::Cratis.Screenplay.Syntax.SliceType.Translate, Direction = TranslationDirection.Outbound }
            : selected with { Events = [selected.Events.First() with { Visibility = EventVisibility.Public, Origin = form == "foreign" ? "producer" : null }] };
        feature = feature with { Slices = [selected] };
        module = module with { Features = [feature] };
        _application = _application with { Modules = [module] };
        var context = new ApplicationSet([_application]);
        foreach (var scope in new[] { "application", "module", "feature", "slice" })
        {
            var error = await Catch.Exception(() => scope switch
            {
                "application" => _renderer.Render([_application], _targetDirectory, _output, _error),
                "module" => _renderer.Render(module, context, _targetDirectory, _output, _error),
                "feature" => _renderer.Render(feature, context, _targetDirectory, _output, _error),
                _ => _renderer.Render(selected, context, _targetDirectory, _output, _error)
            });
            error.ShouldBeOfExactType<RenderingFailed>();
            ((RenderingFailed)error).Failures.Single().Message.StartsWith(code + ":", StringComparison.Ordinal).ShouldBeTrue();
            _scaffolder.WasCalled.ShouldBeFalse();
            _codeOutput.Files.ShouldBeEmpty();
        }
    }
}
