// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticTranslationSpecificationRenderer
{
    static void RenderAssertions(CSharpCodeBuilder builder, SemanticSpecification specification, SemanticApplicationContext context, SemanticTypeSystem types, bool outbound)
    {
        var actual = outbound ? "_published" : "_produced";
        builder.Line($"[global::Xunit.FactAttribute] void should_produce_exactly_the_expected_events() => {actual}.Count.ShouldEqual({specification.ThenEvents.Length});");
        if (outbound)
        {
            builder.Line("[global::Xunit.FactAttribute] void should_not_produce_private_events() => _produced.Count.ShouldEqual(0);");
        }
        else
        {
            builder.Line("[global::Xunit.FactAttribute] void should_not_publish_public_events() => _published.Count.ShouldEqual(0);");
        }
        builder.Line("[global::Xunit.FactAttribute] void should_not_append_directly_to_the_private_log() => _log.DidNotReceiveWithAnyArgs().Append(default!, default!);");
        foreach (var (expected, index) in specification.ThenEvents.Select((expected, index) => (expected, index)))
        {
            RenderEventAssertions(builder, expected, index, actual, context, types);
        }
    }

    static void RenderEventAssertions(CSharpCodeBuilder builder, SemanticSpecificationEvent expected, int index, string actual, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        var entry = $"{actual}[{index}]";
        var contract = context.Events[expected.EventContract];
        builder.Line($"[global::Xunit.FactAttribute] void should_produce_event_{index + 1}_of_the_expected_type() => ({entry}.Event is {types.EventType(contract)}).ShouldBeTrue();");
        if (expected.EventSource is { } identity)
        {
            builder.Line($"[global::Xunit.FactAttribute] void should_preserve_destination_{index + 1}() => {entry}.EventSourceId.ShouldEqual((global::Cratis.Chronicle.Events.EventSourceId){Source(identity, types)});");
        }
        foreach (var property in contract.Properties)
        {
            var value = expected.Values.Single(value => value.TargetProperty == property.Id).Value;
            var member = $"@event.{context.MemberNames.Property(property)}";
            string predicate;
            if (property.Type.IsCollection && value is not SemanticNullValue)
            {
                var name = $"_expected_translation_event_{index}_{Identifiers.ToSnakeCase(property.Name)}";
                builder.Line($"static readonly {types.Type(property.Type)} {name} = {types.Value(value, property.Type)};");
                predicate = $"{member} is {{ }} actual && actual.SequenceEqual({name})";
            }
            else
            {
                predicate = value is SemanticNullValue ? $"{member} is null" : $"{member} == {types.Value(value, property.Type)}";
            }
            builder.Line($"[global::Xunit.FactAttribute] void should_produce_event_{index + 1}_with_{Identifiers.ToSnakeCase(property.Name)}() => ({entry}.Event is {types.EventType(contract)} @event && {predicate}).ShouldBeTrue();");
        }
    }
}
