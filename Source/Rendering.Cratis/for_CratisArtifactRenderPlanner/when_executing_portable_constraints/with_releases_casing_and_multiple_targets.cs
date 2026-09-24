// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_portable_constraints.with_releases_casing_and_multiple_targets.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_portable_constraints;

/// <summary>
/// Exercises accepted and rejected constraint histories in the generated application.
/// </summary>
/// <param name="fixture">The generated application fixture.</param>
public class with_releases_casing_and_multiple_targets(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_pass_generated_specs() => fixture.Results.All(result => result.Outcome == "Passed").ShouldBeTrue();
    [Theory]
    [InlineData("when_released_by_owner")]
    [InlineData("when_ignoring_description_casing")]
    [InlineData("when_imported_value_is_taken")]
    [InlineData("when_reclaim_own_description")]
    void should_execute_each_history(string name) => fixture.Results.Any(result => result.Name.Contains(name, StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace(
                "      event InvoiceIssued\n",
                DedentFour("""
                      constraint UniqueDescription
                        unique description on InvoiceIssued
                        unique description on InvoiceImported
                        released by InvoiceReleased
                        ignore casing
                      event InvoiceIssued
            """) + "\n",
                StringComparison.Ordinal)
            .Replace(
                "      specification IssuingSecondInvoice\n",
                DedentFour("""
                      specification ReleasedByOwner
                        given InvoiceIssued
                          for "invoice-one"
                          description = "First payload"
                        given InvoiceReleased
                          description = "First payload"
                        when IssueInvoice
                          description = "First payload"
                          streamReference = "invoice-two"
                        then InvoiceIssued
                          description = "First payload"
                      specification IgnoringDescriptionCasing
                        given InvoiceIssued
                          for "invoice-one"
                          description = "First payload"
                        when IssueInvoice
                          description = "FIRST PAYLOAD"
                          streamReference = "invoice-two"
                        then error "Constraint 'UniqueDescription' is violated: another event source already holds the constrained value."
                      specification ImportedValueIsTaken
                        given InvoiceImported
                          description = "First payload"
                        when IssueInvoice
                          description = "First payload"
                          streamReference = "invoice-two"
                        then error "Constraint 'UniqueDescription' is violated: another event source already holds the constrained value."
                      specification ReclaimOwnDescription
                        given InvoiceIssued
                          for "invoice-one"
                          description = "First payload"
                        when IssueInvoice
                          description = "First payload"
                          streamReference = "invoice-one"
                        then InvoiceIssued
                          description = "First payload"
                      specification IssuingSecondInvoice
            """) + "\n",
                StringComparison.Ordinal) + """

                slice StateChange Import
                  command ImportInvoice
                    streamReference String identifier
                    description String
                    produces InvoiceImported
                      for streamReference
                      description = description
                  event InvoiceImported
                    description String
                slice StateChange Release
                  command ReleaseInvoice
                    streamReference String identifier
                    description String
                    produces InvoiceReleased
                      for streamReference
                      description = description
                  event InvoiceReleased
                    description String
            """;

        protected override ArtifactRenderPlan CreatePlan()
        {
            // Screenplay's source binder cannot infer the destination type of a cross-slice given event.
            // Supply the same concrete destination the reference execution and generated log both consume.
            var original = invoice_model.Compile(InvoiceSource);
            var module = original.Application.Modules.Single();
            var feature = module.Features.Single();
            var issue = feature.Slices.Single(slice => slice.Name == "Issue");
            var identifier = issue.Commands.Single().Properties.Single(property => property.IsIdentifier).Type;
            var givenSource = new SemanticEventSourceIdentity(identifier, new SemanticTextValue("invoice-one"));
            var changed = issue with { Specifications = [.. issue.Specifications.Select(specification => specification with
            {
                GivenEvents = [.. specification.GivenEvents.Select(given => given.EventSource is null ? given with { EventSource = givenSource } : given)]
            })] };
            var model = ExecutableSemanticModel.Create(
                original.LanguageVersion,
                original.SemanticVersion,
                original.Application with { Modules = [module with { Features = [feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice.Id == issue.Id ? changed : slice)]
                }] }] });
            return invoice_model.Plan(model);
        }

        static string DedentFour(string text) => string.Join('\n', text.Split('\n').Select(line => line.Length >= 4 ? line[4..] : line));

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
