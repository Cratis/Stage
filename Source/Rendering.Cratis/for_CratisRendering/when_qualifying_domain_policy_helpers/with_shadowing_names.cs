// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_qualifying_domain_policy_helpers;

public class with_shadowing_names : Specification
{
    string _source = null!;
    readonly List<CratisPlanDiagnostic> _diagnostics = [];
    readonly List<string> _errors = [];
    readonly List<string> _warnings = [];
    readonly List<string> _policies = [];

    void Establish() => _source = opaque_policy_model.Source("return context.Subject != \"\" && context.Identity.HasRole(\"Staff\");", "Custom and Owner and Excluded", "Custom and Owner and Excluded") + "\n" + """
        policy Owner
          require claim "owner" matches invoiceId
        policy Excluded
          require not (claim "owner" matches subject and (role "Banned" or claim "owner" matches invoiceId))
        """;

    void Because()
    {
        var options = new CratisPlanOptions("InvoiceModel", "InvoiceApp", "Invoices") { Domain = "Sales/Retail" };
        var scaffold = CratisRendering.PlanScaffold(options with { Domain = string.Empty });
        foreach (var (declaration, name) in new[] { ("module", "PolicyValues"), ("feature", "PolicyValues"), ("module", "GeneratedPolicies"), ("feature", "GeneratedPolicies") })
        {
            var source = _source.Replace(declaration == "module" ? "module Billing" : "feature Invoicing", $"{declaration} {name}", StringComparison.Ordinal);
            var loaded = opaque_policy_model.Load(source);
            var plan = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Module(declaration == "module" ? name : "Billing")]), options);
            _diagnostics.AddRange(plan.Diagnostics);
            var files = plan.Artifacts.Concat(scaffold.Artifacts)
                .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
                .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
            _errors.AddRange(RenderedOutput.Errors(files));
            _warnings.AddRange(RenderedOutput.Warnings(files));
            _policies.AddRange(files.Where(file => Path.GetFileName(file.RelativePath).StartsWith("StagePolicy_", StringComparison.Ordinal)).Select(file => file.Content));
        }
    }

    [Fact] void should_admit_all_shadowing_models() => _diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_compile_all_domain_policies() => _errors.ShouldBeEmpty();
    [Fact] void should_compile_without_warnings() => _warnings.ShouldBeEmpty();
    [Fact] void should_emit_both_operation_policies_for_every_case() => _policies.Count.ShouldEqual(8);
    [Fact] void should_qualify_every_root_value_helper() => _policies.TrueForAll(policy => new[] { "Match", "Path", "Query", "Truth", "Not", "And", "Or" }.All(method => !policy.Replace("global::Invoices.GeneratedPolicies.PolicyValues.", "", StringComparison.Ordinal).Contains($"PolicyValues.{method}", StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_qualify_opaque_body_calls() => _policies.TrueForAll(policy => policy.Contains("global::Invoices.Sales.Retail.GeneratedPolicies.PolicyBodies.Evaluate_", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_import_the_shadowable_root_generated_namespace() => _policies.TrueForAll(policy => !policy.Contains("using Invoices.GeneratedPolicies;", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_qualify_the_root_registry() => _policies.TrueForAll(policy => policy.Contains("global::Invoices.GeneratedPolicies.Registration.Add(", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
