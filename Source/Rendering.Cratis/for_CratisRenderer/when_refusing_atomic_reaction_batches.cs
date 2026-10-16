// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_refusing_atomic_reaction_batches : a_multi_slice_application
{
    void Establish()
    {
        var compilation = new ScreenplayCompiler().Compile("""
            reaction batches atomic
            module Billing
              feature Invoices
                slice Automation Complete
                  event Started
                  event Completed
                  reaction Complete
                    when Started
                      produces Completed
            """);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _application = compilation.Value!;
    }

    [Theory]
    [InlineData("application")]
    [InlineData("module")]
    [InlineData("feature")]
    [InlineData("slice")]
    public async Task should_refuse_before_scaffolding_or_writing_any_file(string scope)
    {
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var context = new ApplicationSet([_application]);
        var exception = await Catch.Exception(() => scope switch
        {
            "application" => _renderer.Render([_application], _targetDirectory, _output, _error),
            "module" => _renderer.Render(module, context, _targetDirectory, _output, _error),
            "feature" => _renderer.Render(feature, context, _targetDirectory, _output, _error),
            _ => _renderer.Render(slice, context, _targetDirectory, _output, _error)
        });
        var failure = Assert.IsType<RenderingFailed>(exception).Failures.Single();
        failure.ShouldBeOfExactType<InvalidEventModel>();
        failure.Message.ShouldContain("STAGE-ESM-043:");
        failure.Message.ShouldContain("atomic");
        _error.ToString().ShouldContain("STAGE-ESM-043:");
        _scaffolder.WasCalled.ShouldBeFalse();
        _codeOutput.Files.ShouldBeEmpty();
        _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
    }
}
