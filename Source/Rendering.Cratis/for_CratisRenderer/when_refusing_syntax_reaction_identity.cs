// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_refusing_syntax_reaction_identity : a_multi_slice_application
{
    [Theory]
    [InlineData("identity", "STAGE-ESM-034")]
    [InlineData("filter", "STAGE-ESM-035")]
    public async Task should_refuse_every_entrypoint_before_any_output(string form, string code)
    {
        var location = SourceLocation.Start;
        var reaction = new ReactionSyntax("Follow", [], location)
        {
            RunsAs = form == "identity" ? new("system", ["Automation"], location) : null,
            From = form == "filter" ? new("Account", "Notes", location) : null
        };
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var selected = feature.Slices.First() with { Reactions = [reaction] };
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
