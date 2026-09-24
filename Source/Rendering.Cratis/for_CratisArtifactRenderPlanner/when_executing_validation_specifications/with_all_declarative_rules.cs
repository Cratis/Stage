// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_validation_specifications.with_all_declarative_rules.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_validation_specifications;

public class with_all_declarative_rules(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_pass_the_acceptance_and_every_rejection() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_execute_each_modeled_specification() => fixture.Results.Length.ShouldEqual((17 * 3) + 4);
    [Fact] void should_render_each_authored_rejection_message() => Rules.Where(_ => _.Rule.Length > 0).All(_ => fixture.Generated.Contains($".WithMessage(\"{_.Message}\")", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_assert_the_first_rejection_message() => fixture.GeneratedRejection.ShouldContain("_result.ValidationResults.First().Message.ShouldEqual(\"minimum must be less than maximum\")");
    [Fact] void should_match_with_the_reference_regex_options_and_timeout() => fixture.Generated.ShouldContain("RegexOptions.ECMAScript | System.Text.RegularExpressions.RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)");

    public class context : a_generated_invoice_application
    {
        public string Generated { get; private set; } = null!;
        public string GeneratedRejection { get; private set; } = null!;
        protected override string InvoiceSource => ValidationSource();

        async Task Because()
        {
            Generated = ReadGeneratedFile("Billing/Invoicing/Issue/Issue.cs");
            GeneratedRejection = ReadGeneratedFile("Billing/Invoicing/Issue/when_rejecting_requirement.cs");
            await VerifyGeneratedApplication();
        }
    }

    static readonly (string Name, string Type, string Valid, string Rule, string Invalid, string Message)[] Rules =
    [
        ("present", "String", "\"present\"", "not empty", "\"\"", "present is required"),
        ("shortText", "String", "\"abc\"", "max 3", "\"abcd\"", "short text is too long"),
        ("longText", "String", "\"ab\"", "min 2", "\"a\"", "long text is too short"),
        ("maximum", "Decimal", "10", "max 10", "11", "maximum exceeded"),
        ("minimum", "Decimal", "1", "min 1", "0", "minimum not met"),
        ("equalValue", "Bool", "true", "== true", "false", "equal value differs"),
        ("different", "String", "\"allowed\"", "!= \"blocked\"", "\"blocked\"", "different value is blocked"),
        ("greater", "Int", "1", "> 0", "0", "greater value is too low"),
        ("atLeast", "Decimal", "0", ">= 0", "-1", "at least value is too low"),
        ("less", "Int", "9", "< 10", "10", "less value is too high"),
        ("atMost", "Int", "10", "<= 10", "11", "at most value is too high"),
        ("exactText", "String", "\"abc\"", "length == 3", "\"ab\"", "exact text length differs"),
        ("above", "Int[]", "[1]", "all > 0", "[0]", "above contains zero"),
        ("nonnegative", "Decimal[]", "[0]", "all >= 0", "[-1]", "nonnegative contains a negative"),
        ("pattern", "String", "\"AX\"", "matches \"^A\"", "\"BX\"", "pattern did not match"),
        ("optionalText", "String?", "\"ab\"", "min 2", "\"x\"", "optional text is too short")
    ];

    static string ValidationSource()
    {
        var lines = new List<string>
        {
            "module Billing",
            "  feature Invoicing",
            "    slice StateChange Issue",
            "      command IssueInvoice",
            "        streamReference String identifier",
            "        description String"
        };
        lines.AddRange(Rules.Select(_ => $"        {_.Name} {_.Type}"));
        lines.Add("        validate");
        lines.AddRange(Rules.Where(_ => _.Rule.Length > 0).Select(_ => $"          {_.Name} {_.Rule} message \"{_.Message}\""));
        lines.AddRange(
        [
            "          require minimum < maximum",
            "            message \"minimum must be less than maximum\"",
            "        produces InvoiceIssued",
            "          for streamReference",
            "          description = description",
            "      event InvoiceIssued",
            "        description String"
        ]);
        void AddSpecification(string name, string? replacement, string? invalid, string? message)
        {
            lines.Add($"      specification {name}");
            lines.Add("        when IssueInvoice");
            lines.Add("          streamReference = \"invoice-one\"");
            lines.Add("          description = \"Accepted\"");
            lines.AddRange(Rules.Select(_ => $"          {_.Name} = {(_.Name == replacement ? invalid : _.Valid)}"));
            if (message is null)
            {
                lines.Add("        then InvoiceIssued");
                lines.Add("          description = \"Accepted\"");
            }
            else
            {
                lines.Add($"        then error \"{message}\"");
            }
        }

        AddSpecification("AcceptingBoundaryValues", null, null, null);
        AddSpecification("AcceptingWhitespaceAsNonEmpty", "present", "\" \"", null);
        foreach (var rule in Rules)
        {
            AddSpecification($"Rejecting{char.ToUpperInvariant(rule.Name[0])}{rule.Name[1..]}", rule.Name, rule.Invalid, rule.Message);
        }

        // Both values meet their property rules but fail the command-wide requirement.
        AddSpecification("RejectingRequirement", "minimum", "10", "minimum must be less than maximum");
        return string.Join('\n', lines);
    }
}
#endif
