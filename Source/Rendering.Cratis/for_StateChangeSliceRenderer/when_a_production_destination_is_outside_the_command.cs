// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_a_production_destination_is_outside_the_command : state_change_slices
{
    [Theory]
    [InlineData("invoiceId.Value")]
    [InlineData("missing")]
    public void should_refuse_the_destination_instead_of_redirecting_the_event(string path)
    {
        var command = _registerInvoice.Slice.Commands.Single();
        var produced = command.Produces.Single() with { For = new PathExpressionSyntax(path, SourceLocation.Start) };
        var slice = _registerInvoice with { Slice = _registerInvoice.Slice with { Commands = [command with { Produces = [produced] }] } };
        var error = Catch.Exception(() => new StateChangeSliceRenderer().Render(slice, _applicationSet, "Generated"));
        error.ShouldBeOfExactType<UnsupportedExpression>();
    }
}
