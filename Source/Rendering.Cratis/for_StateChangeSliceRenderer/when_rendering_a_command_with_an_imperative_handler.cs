// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_a_command_with_an_imperative_handler : state_change_slices
{
    RenderedFile _file = null!;
    StateChangeSliceRenderer _renderer = null!;

    void Establish() => _renderer = new StateChangeSliceRenderer();

    void Because() => _file = _renderer.Render(_processBatch, _applicationSet, "CratisApp");

    [Fact] void should_import_command_context() => _file.Content.ShouldContain("using Cratis.Arc.Commands;");
    [Fact] void should_declare_handle_with_a_command_context_parameter() =>
        _file.Content.ShouldContain("public IEnumerable<object> Handle(CommandContext context)");
    [Fact] void should_embed_the_handler_code_verbatim() => _file.Content.ShouldContain("events.Add(new BatchProcessed(context.Identity.Id));");
    [Fact] void should_not_emit_a_validator_when_there_are_no_declared_rules() => _file.Content.ShouldNotContain("Validator");
}
