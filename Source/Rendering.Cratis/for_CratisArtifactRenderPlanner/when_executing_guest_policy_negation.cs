// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_guest_policy_negation.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Exercises guest opt-in through the generated Arc command and query pipelines, not just the policy predicate.
/// </summary>
public class when_executing_guest_policy_negation(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_and_release_without_warnings() => (fixture.DebugWarnings + fixture.ReleaseWarnings).ShouldBeEmpty();
    [Fact] void should_run_every_generated_guest_and_authenticated_vector() => fixture.Results.Length.ShouldEqual(32);
    [Fact] void should_match_the_reference_through_the_generated_pipelines() => fixture.Results.All(result => result.Outcome == "Passed").ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => Source;

        internal static string Source
        {
            get
            {
                (string Condition, string Type, string? Value, bool Allowed)[] policies =
                [
                    ("not role \"Banned\"", "String", null, true),
                    ("not authenticated", "String", null, true),
                    ("not claim \"owner\" matches owner", "String", null, false),
                    ("not claim \"owner\" matches owner", "Int", "42", false),
                    ("not claim \"owner\" matches \"person\"", "String", null, true),
                    ("not claim \"owner\" matches owner", "String", "\"person\"", true),
                    ("not (role \"Banned\" and claim \"owner\" matches owner)", "Int", "42", true),
                    ("not (role \"Banned\" or claim \"owner\" matches owner)", "Int", "42", false)
                ];
                var source = new StringBuilder();
                for (var index = 0; index < policies.Length; index++)
                {
                    source.Append($"policy P{index}\n  require {policies[index].Condition}\n");
                }

                source.Append("concept ReportId : String\npolicy Guests\n  require not authenticated\npolicy Staff\n  require authenticated\nmodule Billing\n  feature Invoicing\n");
                for (var index = 0; index < policies.Length; index++)
                {
                    var (_, type, value, allowed) = policies[index];
                    var owner = $"\n          owner = {value ?? "null"}";
                    var then = allowed ? $"then Filed{index}\n          for \"r-1\"\n          reportId = \"r-1\"" : "then denied";
                    source.Append($$"""
                            slice StateChange File{{index}}
                              command File{{index}}
                                reportId ReportId identifier
                                owner {{type}} optional
                                authorize P{{index}}
                                produces Filed{{index}}
                                  for reportId
                                  reportId = reportId
                              event Filed{{index}}
                                reportId ReportId
                              specification Guest{{index}}
                                given caller
                                when File{{index}}
                                  reportId = "r-1"{{owner}}
                                {{then}}

                        """);
                }

                source.Append("""
                      specification BannedCaller
                        given caller
                          authenticated
                          role "Banned"
                        when File0
                          reportId = "r-1"
                          owner = "person"
                        then denied
                      specification SignedInCaller
                        given caller
                          authenticated
                        when File1
                          reportId = "r-1"
                          owner = "person"
                        then denied
                    slice StateView Lookup
                      readmodel Report
                        reportId ReportId
                      query GuestReport => Report?
                        by reportId ReportId
                        authorize Guests
                      query NoGuestReport => Report?
                        by reportId ReportId
                        authorize Guests and Staff
                      query EitherReport => Report?
                        by reportId ReportId
                        authorize Guests or Staff
                      projection Reports => Report
                        from Filed0 key reportId
                          reportId = reportId
                      specification GuestQuery
                        given caller
                        given Filed0
                          for "r-1"
                          reportId = "r-1"
                        then query GuestReport
                          arguments
                            reportId = "r-1"
                          result
                            reportId = "r-1"
                      specification DeniedGuestQuery
                        given caller
                        then query NoGuestReport
                          arguments
                            reportId = "r-1"
                        then denied
                      specification AlternativeGuestQuery
                        given caller
                        given Filed0
                          for "r-1"
                          reportId = "r-1"
                        then query EitherReport
                          arguments
                            reportId = "r-1"
                          result
                            reportId = "r-1"
                """);

                return source.ToString();
            }
        }

        async Task Because()
        {
            var model = invoice_model.Compile(Source);
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            foreach (var specification in model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications))
            {
                var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
                Assert.True(reference.Passed, $"{specification.Name}: {string.Join(';', reference.Failures)}");
            }

            AddGeneratedSpecification("GuestRegistrations.cs", """
                #if DEBUG
                using System.Reflection;
                using Cratis.Arc.Authorization;
                using Microsoft.Extensions.DependencyInjection;
                using Xunit;
                namespace Invoices.Probes;
                public class GuestRegistrations
                {
                    [Fact]
                    public void should_opt_in_only_operations_with_a_definite_guest_allow()
                    {
                        var services = new ServiceCollection();
                        Invoices.GeneratedPolicies.Registration.Register(services);
                        var policies = services.Where(service => service.ImplementationInstance is AuthorizationPolicyRegistration)
                            .Select(service => (AuthorizationPolicyRegistration)service.ImplementationInstance!).ToArray();
                        Assert.Equal(11, policies.Length);
                        (string Operation, bool Anonymous)[] expected =
                        [
                            ("File0", true), ("File1", true), ("File2", true), ("File3", false),
                            ("File4", true), ("File5", true), ("File6", true), ("File7", false),
                            ("GuestReport", true), ("NoGuestReport", false), ("EitherReport", true)
                        ];
                        var types = typeof(GuestRegistrations).Assembly.GetTypes();
                        foreach (var (operation, anonymous) in expected)
                        {
                            MemberInfo target = operation.StartsWith("File", StringComparison.Ordinal)
                                ? types.Single(type => type.Name == operation)
                                : types.Single(type => type.Name == "Report").GetMethod(operation)!;
                            var name = target.GetCustomAttribute<AuthorizeAttribute>()!.Policy;
                            Assert.Equal(anonymous, policies.Single(policy => policy.Name == name).EvaluatesAnonymous);
                        }
                    }
                }
                #endif
                """);
            await VerifyGeneratedApplication();
        }
    }
}
#endif
