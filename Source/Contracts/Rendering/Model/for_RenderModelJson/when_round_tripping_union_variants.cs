// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelJson;

public class when_round_tripping_union_variants : given.render_models
{
    static readonly RenderTypeRef Text = new(RenderTypeKind.Primitive, SemanticPrimitiveType.Text, null, false, false);
    static readonly RenderTypeRef Boolean = new(RenderTypeKind.Primitive, SemanticPrimitiveType.Boolean, null, false, false);
    static readonly RenderExpression Value = new RenderExpression.Value(new RenderLiteral.Text("æøå"), Text);

    [Fact]
    void should_preserve_every_expression_variant()
    {
        var id = Basic().Items.Commands[0].Id;
        RoundTripAll<RenderExpression>([
            Value,
            new RenderExpression.Value(new RenderLiteral.Null(), null),
            new RenderExpression.Resolved(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, id, Text),
            new RenderExpression.EventContext(SemanticEventContextValueKind.EventSourceIdentity, Text),
            new RenderExpression.Refusal("message", Text),
            new RenderExpression.ReadPath("customer", id, Text),
            new RenderExpression.Comparison(Value, SemanticComparisonOperator.Equal, Value, Boolean),
            new RenderExpression.Logical(Value, SemanticLogicalOperator.And, Value, Boolean)
        ]);
    }

    [Fact]
    void should_preserve_every_literal_variant()
    {
        var id = Basic().Items.Commands[0].Id;
        RoundTripAll<RenderLiteral>([new RenderLiteral.Null(), new RenderLiteral.Text("text"), new RenderLiteral.Number(9007199254740992m), new RenderLiteral.Boolean(true), new RenderLiteral.Array(new([new RenderLiteral.Text("last"), new RenderLiteral.Number(-2.5m)])), new RenderLiteral.Composite(new([new(id, new RenderLiteral.Null())]))]);
    }

    [Fact]
    void should_preserve_every_authorization_variant() => RoundTripAll<RenderAuthorization>([new RenderAuthorization.Authenticated(), new RenderAuthorization.Anonymous(), new RenderAuthorization.Role("operator"), new RenderAuthorization.Claim("tenant", SemanticClaimTargetKind.Literal, "example"), new RenderAuthorization.Policy("members", new RenderAuthorization.Authenticated()), new RenderAuthorization.OpaquePolicy("requirement"), new RenderAuthorization.NotPolicy(new RenderAuthorization.Anonymous()), new RenderAuthorization.Logical(new RenderAuthorization.Authenticated(), SemanticLogicalOperator.Or, new RenderAuthorization.Role("operator"))]);

    [Fact]
    void should_preserve_every_response_variant()
    {
        var id = Basic().Items.Commands[0].Id;
        RoundTripAll<RenderResponse>([new RenderResponse.Absent(), new RenderResponse.Scalar(id, Text), new RenderResponse.Record(new([new("message", id, Text)]))]);
    }

    [Fact]
    void should_reject_unknown_union_discriminators() => Catch.Exception(() => JsonSerializer.Deserialize<RenderLiteral>("{\"$kind\":\"unclassified\"}", RenderModelJson.Options)).ShouldBeOfExactType<JsonException>();

    static void RoundTripAll<T>(T[] variants)
    {
        foreach (var variant in variants)
        {
            RoundTrip(variant);
            ValidateVariant(variant);
        }
    }

    static void ValidateVariant<T>(T variant)
    {
        var model = WithPayload(payload => variant switch
        {
            RenderExpression expression => payload with { Requirements = new([new(expression, null, SemanticValidationSeverity.Error)]) },
            RenderLiteral literal => payload with { Requirements = new([new(new RenderExpression.Value(literal, null), null, SemanticValidationSeverity.Error)]) },
            RenderAuthorization authorization => payload with { Authorization = authorization },
            RenderResponse response => payload with { Response = response },
            _ => throw new InvalidOperationException("Unclassified schema fixture.")
        });
        var schema = JsonNode.Parse(File.ReadAllBytes(for_RenderModelSchema.given.render_model_schema.FilePath("render-model.v1.schema.json")))!;
        using var document = JsonDocument.Parse(RenderModelJson.Serialize(model));
        new for_RenderModelSchema.given.strict_schema_validator(schema).Validate(document.RootElement).ShouldBeTrue();
    }

    static void RoundTrip<T>(T variant)
    {
        var json = RenderModelJson.Serialize(variant);
        var copy = JsonSerializer.Deserialize<T>(json, RenderModelJson.Options);
        copy.ShouldEqual(variant);
        EqualBytes(json, RenderModelJson.Serialize(copy));
    }
}
