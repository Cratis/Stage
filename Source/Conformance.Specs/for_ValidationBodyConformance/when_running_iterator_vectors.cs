// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using FluentValidation;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ValidationBodyConformance;

public class when_running_iterator_vectors : Specification
{
    [Theory]
    [InlineData("yield return \"First\"; yield return \"Second\";", "fr-FR", "First", "Second")]
    [InlineData("yield return $\"{context.Artifact.Amount:N2}|{context.Artifact.RecordedAt:yyyy-MM-dd}\";", "fr-FR", "1,234.50|2026-10-17", null)]
    [InlineData("yield return $\"{context.Artifact.Amount:N2}|{context.Artifact.RecordedAt:yyyy-MM-dd}\";", "ar-SA", "1,234.50|2026-10-17", null)]
    void should_execute_every_yield_and_preserve_invariant_message_output(string body, string culture, string first, string? second)
    {
        var source = $$"""
            concept OrderId : Uuid
            module Orders
              feature Ordering
                slice StateChange PlaceOrder
                  command PlaceOrder
                    orderId OrderId generated identifier
                    amount Decimal
                    recordedAt DateTime
                    validate csharp
                      ```csharp
                      {{body}}
                      ```
                    produces OrderPlaced
                      for orderId
                      amount = amount
                      recordedAt = recordedAt
                  event OrderPlaced
                    amount Decimal
                    recordedAt DateTime
            """;
        var loaded = opaque_policy_model.Load(source);
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var assembly = opaque_policy_model.Compile(plan);
        var type = assembly.GetTypes().Single(type => type.Name == "PlaceOrder");
        var command = Activator.CreateInstance(type, 1234.5m, new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero))!;
        var validator = (IValidator)Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == "PlaceOrderValidator"))!;
        var context = (IValidationContext)Activator.CreateInstance(typeof(ValidationContext<>).MakeGenericType(type), command)!;
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var result = validator.Validate(context);
            result.IsValid.ShouldBeFalse();
            Assert.Equal(second is null ? [first] : new[] { first, second }, result.Errors.Select(error => error.ErrorMessage));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
#endif
