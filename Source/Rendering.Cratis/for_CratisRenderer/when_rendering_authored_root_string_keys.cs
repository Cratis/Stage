// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_authored_root_string_keys : a_multi_slice_application
{
    const string Source = """
        module Sales
          feature Orders
            slice StateView Summary
              event OrderCreated
                total Decimal
              projection Order => OrderReadModel
                from OrderCreated
                  key literal "global"
                  total = total
              query OrderById => OrderReadModel
        """;

    async Task Because()
    {
        var compilation = new NativeScreenplayCompiler().Compile(Source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        compilation.Diagnostics.ShouldBeEmpty();
        _application = compilation.Value!;
        IRenderer renderer = _renderer;
        await renderer.Render([_application], _targetDirectory, _output, _error);
    }

    [Fact] void should_emit_the_public_constant_key_property() => Assert.Single(_codeOutput.Files).Content.ShouldContain("[FromEvent<OrderCreated>(ConstantKey = \"global\")]");
    [Fact] void should_use_a_string_lookup_without_changing_the_record() => Assert.Single(_codeOutput.Files).Content.ShouldContain("OrderById(IReadModels readModels, EventSourceId id)");
    [Fact] void should_compile_without_warnings() => RenderedOutput.Warnings(_codeOutput.Files).ShouldBeEmpty();
    [Fact] void should_compile_without_errors() => RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
}
