// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_classifying_semantic_members
{
    [Fact] public void should_classify_every_slice_kind() => AssertCoverage(
        [SemanticSliceKind.StateChange, SemanticSliceKind.StateView],
        [SemanticSliceKind.Unknown]);

    [Fact] public void should_classify_every_type_reference_kind() => AssertCoverage(
        [SemanticTypeReferenceKind.Primitive, SemanticTypeReferenceKind.Concept, SemanticTypeReferenceKind.CompositeType],
        [SemanticTypeReferenceKind.Unknown]);

    [Fact] public void should_classify_every_primitive_type() => AssertCoverage(
        [SemanticPrimitiveType.Uuid, SemanticPrimitiveType.Text, SemanticPrimitiveType.WholeNumber,
            SemanticPrimitiveType.DecimalNumber, SemanticPrimitiveType.Boolean, SemanticPrimitiveType.Date,
            SemanticPrimitiveType.DateTime],
        [SemanticPrimitiveType.Unknown]);

    [Fact] public void should_classify_every_validation_rule_kind() => AssertCoverage(
        [SemanticValidationRuleKind.NotEmpty],
        [SemanticValidationRuleKind.Unknown, SemanticValidationRuleKind.Maximum, SemanticValidationRuleKind.Minimum,
            SemanticValidationRuleKind.Equal, SemanticValidationRuleKind.NotEqual, SemanticValidationRuleKind.GreaterThan,
            SemanticValidationRuleKind.GreaterThanOrEqual, SemanticValidationRuleKind.LessThan,
            SemanticValidationRuleKind.LessThanOrEqual, SemanticValidationRuleKind.Length, SemanticValidationRuleKind.AllGreaterThan,
            SemanticValidationRuleKind.AllGreaterThanOrEqual, SemanticValidationRuleKind.Matches]);

    // A failure rejects at every severity, but only the default error severity renders as stated.
    [Fact] public void should_classify_every_validation_severity() => AssertCoverage(
        [SemanticValidationSeverity.Error],
        [SemanticValidationSeverity.Information, SemanticValidationSeverity.Warning]);

    // Command occurrence values have no exact realization in a command handler; the event-source identity is
    // read from the event context by projections, which the first read capability does not render yet.
    [Fact] public void should_classify_every_event_context_value_kind() => AssertCoverage(
        [],
        [SemanticEventContextValueKind.Unknown, SemanticEventContextValueKind.EventSourceIdentity,
            SemanticEventContextValueKind.Occurred, SemanticEventContextValueKind.CausedBySubject,
            SemanticEventContextValueKind.CausedByName, SemanticEventContextValueKind.CausedByUserName]);

    // Mapping expressions only support resolved properties; specification values are rendered separately.
    [Fact] public void should_classify_every_mapping_expression_kind() => AssertCoverage(
        [SemanticExpressionKind.Resolved],
        [SemanticExpressionKind.Unknown, SemanticExpressionKind.Value, SemanticExpressionKind.EventContext]);

    // Admission accepts null only for optional values and arrays only for collections of scalars.
    // The type emitter can construct composites, but specification admission still rejects them.
    [Fact] public void should_classify_every_value_kind() => AssertCoverage(
        [SemanticValueKind.Null, SemanticValueKind.Text, SemanticValueKind.Number, SemanticValueKind.Boolean,
            SemanticValueKind.Array],
        [SemanticValueKind.Unknown, SemanticValueKind.Composite]);

    static void AssertCoverage<T>(T[] supported, T[] rejected)
        where T : struct, Enum =>
        Assert.Equal(Enum.GetValues<T>(), supported.Concat(rejected).OrderBy(_ => unchecked((uint)Convert.ToInt32(_))));
}
