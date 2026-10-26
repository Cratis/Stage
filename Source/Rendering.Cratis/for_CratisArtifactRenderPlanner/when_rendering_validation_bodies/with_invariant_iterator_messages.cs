// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_invariant_iterator_messages : Specification
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("ar-SA")]
    void should_format_number_and_date_holes_invariantly(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var source = validation_body_model.IteratorCommand("""
                var number = $"{context.Artifact.Amount:N2}";
                yield return $"{number}|{context.Artifact.RecordedAt:yyyy-MM-dd HH:mm:ss zzz}";
                yield return $"{$"{context.Value.Amount:F2}"}|{context.Value.RecordedAt:yyyy-MM-dd}";
                """);
            var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
            Assert.True(plan.Success, opaque_policy_model.Errors(plan));
            var assembly = opaque_policy_model.Compile(plan);
            var result = validation_body_model.Validate(assembly, "PlaceOrder", 1234.5m, new DateTimeOffset(2026, 10, 17, 19, 4, 5, TimeSpan.FromHours(2)));
            Assert.Equal(["1,234.50|2026-10-17 19:04:05 +02:00", "1234.50|2026-10-17"], result.Errors.Select(error => error.ErrorMessage));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("ar-SA")]
    void should_format_concept_iterator_numbers_invariantly(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.IteratorConcept("yield return $\"Amount {context.Value:F2}\";")));
            Assert.True(plan.Success, opaque_policy_model.Errors(plan));
            validation_body_model.Validate(opaque_policy_model.Compile(plan), "Amount", 1234.5m).Errors.Single().ErrorMessage.ShouldEqual("Amount 1234.50");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
#endif
