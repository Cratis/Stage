// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticReactionSpecificationRenderer
{
    static void RenderInvocationExpectations(SpecificationScope scope)
    {
        var expectations = scope.Specification.ThenInvocations;
        if (expectations.IsDefaultOrEmpty) return;
        var builder = scope.Builder;
        builder.Line($"[global::Xunit.FactAttribute] void should_match_authored_invocation_count() => _reactor.Produced.Count.ShouldEqual({expectations.Length});");
        foreach (var (expected, index) in expectations.Select((expected, index) => (expected, index)))
        {
            var command = scope.Context.Commands[expected.Command];
            var commandType = scope.Types.SliceType(command.Id, command.Name);
            var values = new ValueScope(scope.Types, scope.Context.MemberNames, "command", $"_expected_invocation_{index + 1}");
            var fields = expected.Values.Select(value => (command.Properties.Single(property => property.Id == value.TargetProperty), value.Value)).ToArray();
            RenderExpectedCollections(builder, fields, values);
            builder.Line($"[global::Xunit.FactAttribute] void should_match_authored_invocation_{index + 1}_command() => (_reactor.Produced.ElementAtOrDefault({index}) is {commandType}).ShouldBeTrue();");
            foreach (var (property, value) in fields)
            {
                builder.Line($"[global::Xunit.FactAttribute] void should_match_authored_invocation_{index + 1}_{Naming.Identifiers.ToSnakeCase(property.Name)}() => (_reactor.Produced.ElementAtOrDefault({index}) is {commandType} command && {ValuePredicate(property, value, values)}).ShouldBeTrue();");
            }
        }
    }
}
