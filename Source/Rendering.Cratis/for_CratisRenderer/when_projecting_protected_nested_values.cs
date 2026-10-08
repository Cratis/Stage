// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_projecting_protected_nested_values : a_multi_slice_application
{
    [Theory]
    [InlineData("pii", "children lines identified by value")]
    [InlineData("sensitive", "children lines identified by value")]
    [InlineData("pii", "nested details")]
    [InlineData("sensitive", "nested details")]
    public async Task should_not_treat_keys_inside_a_document_as_event_source_identities(string attribute, string block)
    {
        var compilation = new ScreenplayCompiler().Compile($$"""
            concept ProtectedValue : String @{{attribute}}
            module Billing
              feature Invoices
                slice StateView View
                  event Changed
                    id Uuid
                    value ProtectedValue
                  projection View => View
                    from Changed
                      key id
                      id = id
                    {{block}}
                      from Changed key value
                        parent id
                        value = value
            """);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var error = await Catch.Exception(() => _renderer.Render([compilation.Value!], _targetDirectory, _output, _error));
        Assert.True(error is null, $"{error}{Environment.NewLine}{_error}");
        _scaffolder.WasCalled.ShouldBeTrue();
        _codeOutput.Files.ShouldContain(file => file.Content.Contains(attribute == "pii" ? "[PII]" : "[Encrypted]\n[NotAudited]", StringComparison.Ordinal));
        _codeOutput.Files.ShouldContain(file => file.Content.Contains("nameof(Changed.Value)", StringComparison.Ordinal));
        RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
        _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
    }
}
