// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelJson.given;

public class render_models : for_RenderModelBuilder.given.a_response_only_command
{
    internal static RenderModel Basic() => RenderModelBuilder.Build(CompileRequest(Source));

    internal static RenderModel Decimal()
    {
        var number = new RenderTypeRef(RenderTypeKind.Primitive, SemanticPrimitiveType.DecimalNumber, null, false, false);
        var boolean = new RenderTypeRef(RenderTypeKind.Primitive, SemanticPrimitiveType.Boolean, null, false, false);
        var condition = new RenderExpression.Comparison(new RenderExpression.Value(new RenderLiteral.Number(12.3400m), number), SemanticComparisonOperator.GreaterThan, new RenderExpression.Value(new RenderLiteral.Number(0m), number), boolean);

        return WithPayload(payload => payload with { Requirements = new([new(condition, null, SemanticValidationSeverity.Error)]) });
    }

    internal static RenderModel WithPayload(Func<RenderCommandPayload, RenderCommandPayload> transform)
    {
        var model = Basic();
        var command = model.Items.Commands[0];

        return model with { Items = model.Items with { Commands = model.Items.Commands.SetItem(0, command with { Payload = transform(command.Payload) }) } };
    }
    internal static RenderModel Empty()
    {
        var request = CompileRequest(Source);
        var application = request.Model.Application with { Modules = [] };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);

        return RenderModelBuilder.Build(request with { Model = model });
    }
    internal static RenderModel Reaction() => RenderModelBuilder.Build(CompileRequest("""
        module Messaging
          feature Echoes
            slice Automation React
              reaction FirstReaction
                when Startup
        """));

    internal static string Source => """
        module Messaging
          feature Echoes
            slice StateChange Reply
              command FirstEcho
                message String
                returns message
              command SecondEcho
                message String
                returns message
        """;

    internal static void EqualBytes(byte[] expected, byte[] actual)
    {
        var offset = Enumerable.Range(0, Math.Min(expected.Length, actual.Length)).FirstOrDefault(index => expected[index] != actual[index], -1);
        Assert.True(offset == -1 && expected.Length == actual.Length, $"Canonical bytes differ at offset {(offset < 0 ? Math.Min(expected.Length, actual.Length) : offset)}; expected length {expected.Length}, actual length {actual.Length}.");
    }
}
