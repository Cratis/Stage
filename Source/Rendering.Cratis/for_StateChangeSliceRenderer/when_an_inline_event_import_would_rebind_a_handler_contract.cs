// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_an_inline_event_import_would_rebind_a_handler_contract : state_change_slices
{
    [Theory]
    [InlineData("CommandContext", true)]
    [InlineData("Command", true)]
    [InlineData("CommandContext", false)]
    public void should_refuse_instead_of_aliasing_only_the_generated_signature(string collision, bool returned)
    {
        var original = _registerInvoice.Slice.Events.Single();
        var colliding = original with { Name = collision };
        var declaration = _registerInvoice with { Slice = _registerInvoice.Slice with { Events = returned ? [colliding] : [original, colliding] } };
        var eventName = returned ? collision : original.Name;
        var body = $"return new {eventName}(string.Empty);";
        var command = _processBatch.Slice.Commands.Single();
        command = command with { Handler = command.Handler! with { Code = command.Handler.Code! with { Code = body } } };
        var slice = _processBatch with { Slice = _processBatch.Slice with { Commands = [command] } };

        var error = Assert.Throws<CSharpImportRejected>(() => new StateChangeSliceRenderer().Render(slice, ReplaceDeclaration(declaration), "CratisApp"));

        Assert.Contains($"The inferred generated return type '{eventName}' cannot be imported unambiguously for the authored handler.", error.Message);
        Assert.Equal(body, slice.Slice.Commands.Single().Handler!.Code!.Code);
    }

    ApplicationSet ReplaceDeclaration(LocatedSlice declaration)
    {
        var application = _applicationSet.Applications.Single();
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        return new([application with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice.Name == declaration.Slice.Name ? declaration.Slice : slice)] }] }] }]);
    }
}
