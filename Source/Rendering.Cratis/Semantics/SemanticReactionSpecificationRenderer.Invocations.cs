// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticReactionSpecificationRenderer
{
    static void RenderInvocationExpectations(SpecificationScope scope) =>
        RenderInvocationExpectations(scope.Builder, scope.Specification, scope.Context, scope.Types, Recorded(scope.ReactionPrelude!));

    static void RenderInvocationExpectations(CSharpCodeBuilder builder, SemanticSpecification specification, SemanticApplicationContext context, SemanticTypeSystem types, string recorded)
    {
        var expectations = specification.ThenInvocations;
        if (expectations.IsDefaultOrEmpty) return;
        builder.Line($"[global::Xunit.FactAttribute] void should_match_authored_invocation_count() => {recorded}.Count.ShouldEqual({expectations.Length});");
        foreach (var (expected, index) in expectations.Select((expected, index) => (expected, index)))
        {
            var command = context.Commands[expected.Command];
            var commandType = types.SliceType(command.Id, command.Name);
            var values = new ValueScope(types, context.MemberNames, "command", $"_expected_invocation_{index + 1}");
            var fields = expected.Values.Select(value => (command.Properties.Single(property => property.Id == value.TargetProperty), value.Value)).ToArray();
            RenderExpectedCollections(builder, fields, values);
            builder.Line($"[global::Xunit.FactAttribute] void should_match_authored_invocation_{index + 1}_command() => ({recorded}.ElementAtOrDefault({index}) is {commandType}).ShouldBeTrue();");
            foreach (var (property, value) in fields)
            {
                builder.Line($"[global::Xunit.FactAttribute] void should_match_authored_invocation_{index + 1}_{Naming.Identifiers.ToSnakeCase(property.Name)}() => ({recorded}.ElementAtOrDefault({index}) is {commandType} command && {ValuePredicate(property, value, values)}).ShouldBeTrue();");
            }
        }
    }
}
