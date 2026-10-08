// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_preflighting_destination_expressions : a_multi_slice_application
{
    [Theory]
    [InlineData("raw")]
    [InlineData("context")]
    [InlineData("unknown")]
    [InlineData("nested")]
    public async Task should_reject_destinations_with_unproven_lineage_before_any_publication(string kind)
    {
        var compilation = new ScreenplayCompiler().Compile("""
            concept ProtectedValue : String @pii
            module Billing
              feature Invoices
                slice StateChange Change
                  command Change
                    id Uuid identifier
                    value ProtectedValue
                    produces Changed
                      for id
                  event Changed
            """);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var application = compilation.Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        ExpressionSyntax destination = kind switch
        {
            "raw" => new RawExpressionSyntax("Value", SourceLocation.Start),
            "context" => new ContextExpressionSyntax("identity.id", SourceLocation.Start),
            "unknown" => new PathExpressionSyntax("missing", SourceLocation.Start),
            _ => new PathExpressionSyntax("value.Value", SourceLocation.Start),
        };
        slice = slice with { Commands = [command with { Produces = [command.Produces.Single() with { For = destination }] }] };
        feature = feature with { Slices = [slice] };
        module = module with { Features = [feature] };
        application = application with { Modules = [module] };
        var context = new ApplicationSet([application]);
        foreach (var scope in new[] { "application", "module", "feature", "slice" })
        {
            var error = await Catch.Exception(() => scope switch
            {
                "application" => _renderer.Render([application], _targetDirectory, _output, _error),
                "module" => _renderer.Render(module, context, _targetDirectory, _output, _error),
                "feature" => _renderer.Render(feature, context, _targetDirectory, _output, _error, module: "Billing"),
                _ => _renderer.Render(slice, context, _targetDirectory, _output, _error, module: "Billing", feature: "Invoices"),
            });
            error.ShouldBeOfExactType<RenderingFailed>();
            ((RenderingFailed)error).Failures.Single().ShouldBeOfExactType<UnsupportedExpression>();
            _scaffolder.WasCalled.ShouldBeFalse();
            _codeOutput.Files.ShouldBeEmpty();
            _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
            _output.ToString().ShouldBeEmpty();
        }
    }
}
