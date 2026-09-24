// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_validation_specifications.with_concept_rules.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_validation_specifications;

public class with_concept_rules(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_pass_acceptance_and_rejection_specifications() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_execute_every_specification() => fixture.Results.Length.ShouldEqual(10);
    [Fact] void should_render_the_reference_default_message() => fixture.ConceptGenerated.ShouldContain(".WithMessage(\"A value must be at least 2 characters long.\")");

    public class context : a_generated_invoice_application
    {
        public string ConceptGenerated { get; private set; } = null!;
        protected override string InvoiceSource => "concept Display : String\n  validate\n    min 2\nconcept RequiredDisplay : String\n  validate\n    not empty\n" +
            invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
                .Replace("        produces InvoiceIssued\n", "        display Display\n        requiredDisplay RequiredDisplay\n        produces InvoiceIssued\n", StringComparison.Ordinal)
                .Replace("          streamReference = ", "          display = \"xy\"\n          requiredDisplay = \"present\"\n          streamReference = ", StringComparison.Ordinal) +
            "\n      specification RejectingInvalidDisplay\n        when IssueInvoice\n          streamReference = \"invoice-one\"\n          description = \"A description\"\n          display = \"x\"\n          requiredDisplay = \"present\"\n        then error \"A value must be at least 2 characters long.\"\n" +
            "\n      specification RejectingEmptyConcept\n        when IssueInvoice\n          streamReference = \"invoice-one\"\n          description = \"A description\"\n          display = \"xy\"\n          requiredDisplay = \"\"\n        then error \"A required concept value is empty.\"\n";

        async Task Because()
        {
            ConceptGenerated = ReadGeneratedFile("Common/Display.cs");
            await VerifyGeneratedApplication();
        }
    }
}
#endif
