// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_a_file_handler_also_declares_produces : a_file_handler_slice
{
    Exception _error = null!;

    void Establish()
    {
        // Public syntax can be constructed manually; the DSL correctly rejects handler plus produces.
        var command = _command with { Produces = [new ProducesSyntax("BatchProcessed", null, [], SourceLocation.Start)] };
        _slice = _slice with { Slice = _slice.Slice with { Commands = [command] } };
    }

    void Because() => _error = Catch.Exception(() => new StateChangeSliceRenderer().Render(_slice, _applicationSet, "CratisApp"));

    [Fact] void should_reject_instead_of_substituting_declarative_production() => _error.ShouldBeOfExactType<UnsupportedFileBackedCommandHandler>();
    [Fact] void should_keep_the_original_file_reference() => Assert.Same(_command.Handler!.File, ((UnsupportedFileBackedCommandHandler)_error).File);
}
