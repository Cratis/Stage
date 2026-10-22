// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Projections;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Comparison;
using NSubstitute;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_scoped_mapping_lowering_is_refused : Specification
{
    [Theory]
    [InlineData(SemanticProjectionOperation.Set, "SemanticProjectionEventProperty")]
    [InlineData(SemanticProjectionOperation.Add, "SemanticProjectionLiteral")]
    [InlineData(SemanticProjectionOperation.Subtract, "SemanticProjectionLiteral")]
    [InlineData((SemanticProjectionOperation)999, "SemanticProjectionLiteral")]
    public void should_name_the_actual_operation_and_source_kind(SemanticProjectionOperation operation, string sourceKind)
    {
        SemanticProjectionValue source = operation == SemanticProjectionOperation.Set
            ? new SemanticProjectionEventProperty([]) : new SemanticProjectionLiteral(SemanticValue.Number(3));
        var mapping = new SemanticProjectionMapping([], operation, source);
        var from = Substitute.For<IFromBuilder<read_model, mapped_event>>();
        var method = typeof(SemanticRunScopedMappings).GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(read_model), typeof(mapped_event), typeof(long));
        var failure = Assert.Throws<SemanticProjectionLoweringRefused>(() =>
            method.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [from, mapping, nameof(read_model.Value), null], null));
        Assert.Contains(operation.ToString(), failure.Message, StringComparison.Ordinal);
        Assert.Contains(sourceKind, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void should_name_a_literal_that_cannot_be_represented_by_the_runtime_type()
    {
        var mapping = new SemanticProjectionMapping([], SemanticProjectionOperation.Set, new SemanticProjectionLiteral(new SemanticArrayValue([])));
        var from = Substitute.For<IFromBuilder<opaque_read_model, mapped_event>>();
        var method = typeof(SemanticRunScopedMappings).GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(opaque_read_model), typeof(mapped_event), typeof(object));
        var failure = Assert.Throws<SemanticProjectionLoweringRefused>(() =>
            method.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [from, mapping, nameof(opaque_read_model.Value), null], null));
        Assert.Contains("Set", failure.Message, StringComparison.Ordinal);
        Assert.Contains("SemanticProjectionLiteral", failure.Message, StringComparison.Ordinal);
    }

    public sealed class opaque_read_model
    {
        public object Value { get; set; } = null!;
    }

    public sealed class read_model
    {
        public long Value { get; set; }
    }

    public sealed class mapped_event;
}
