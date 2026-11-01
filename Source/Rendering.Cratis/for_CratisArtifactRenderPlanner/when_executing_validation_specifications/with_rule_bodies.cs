// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_validation_specifications;

public class with_rule_bodies(with_rule_bodies.context fixture) : IClassFixture<with_rule_bodies.context>
{
    [Fact] public void should_build_debug_without_warnings() => Assert.Equal(string.Empty, fixture.DebugWarnings);
    [Fact] public void should_build_release_without_warnings() => Assert.Equal(string.Empty, fixture.ReleaseWarnings);
    [Fact] public void should_execute_every_generated_spec() => Assert.Equal(17, fixture.Results.Length);
    [Fact] public void should_pass_every_then_block() => Assert.All(fixture.Results, result => Assert.Equal("Passed", result.Outcome));

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => ModelSource() +
            Specification("Accepting", (5, "Invoice one", "2026-06-02T09:00:00Z"), """
                        then PaymentReceived
                          for "one"
                          amount = 5
                          title = "Invoice one"
                          due = "2026-06-02T09:00:00Z"
                """) +
            Specification("RejectingAmount", (0, "Invoice one", "2026-06-02T09:00:00Z"), "        then error \"Amount must be positive\"") +
            Specification("RejectingTitle", (5, "Wrong", "2026-06-02T09:00:00Z"), "        then error \"Title must start with Invoice\"") +
            Specification("RejectingDue", (5, "Invoice one", "2026-05-01T09:00:00Z"), "        then error \"Due must be in the future\"");

        protected override ArtifactRenderPlan CreatePlan() => opaque_policy_model.Plan(validation_body_model.Load(InvoiceSource));

        static string Specification(string name, (int Amount, string Title, string Due) input, string then) => "\n" + $$"""
                  specification {{name}}
                    given clock "2026-06-01T09:00:00Z"
                    when ReceivePayment
                      id = "one"
                      amount = {{input.Amount}}
                      title = "{{input.Title}}"
                      due = "{{input.Due}}"
            """ + "\n" + then + "\n";

        static string ModelSource() => """
            eventsource VersionMarker
              identifier String
            concept InvoiceId : String
            module Billing
              feature Receipts
                slice StateChange Receiving
                  command ReceivePayment
                    id InvoiceId identifier
                    amount Int
                    title String
                    due DateTime
                    validate
                      amount rule Positive message "Amount must be positive"
                        ```csharp
                        return (int)context.Value > 0;
                        ```
                      title rule Title message "Title must start with Invoice"
                        ```csharp
                        return context.Value.StartsWith("Invoice");
                        ```
                      due rule Future message "Due must be in the future"
                        ```csharp
                        return (DateTimeOffset)context.Value > context.Occurred;
                        ```
                    produces PaymentReceived
                      for id
                      amount = amount
                      title = title
                      due = due
                  event PaymentReceived
                    amount Int
                    title String
                    due DateTime
            """;

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
