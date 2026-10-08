// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

/// <summary>
/// Pins guest negation outcomes against both Stage's in-memory executor and Screenplay's reference runner.
/// </summary>
public class with_guest_policy_negation : Specification
{
    SemanticExecutionPlan _plan = null!;
    readonly List<string> _mismatches = [];
    int _vectors;

    void Establish()
    {
        (string Condition, string Type, string Value, string Caller, bool Allowed)[] policies =
        [
            ("not role \"Banned\"", "String", "\"person\"", string.Empty, true),
            ("not authenticated", "String", "\"person\"", string.Empty, true),
            ("not claim \"owner\" matches owner", "Decimal", "42.5", string.Empty, false),
            ("not claim \"owner\" matches owner", "Int", "42", string.Empty, false),
            ("not claim \"owner\" matches \"person\"", "String", "\"person\"", string.Empty, true),
            ("not claim \"owner\" matches owner", "String", "\"person\"", string.Empty, true),
            ("not (role \"Banned\" and claim \"owner\" matches owner)", "Int", "42", string.Empty, true),
            ("not (role \"Banned\" or claim \"owner\" matches owner)", "Int", "42", string.Empty, false),
            ("not role \"Banned\"", "String", "\"person\"", "\n          authenticated\n          role \"Banned\"", false),
            ("not authenticated", "String", "\"person\"", "\n          authenticated", false)
        ];
        var source = new StringBuilder();
        for (var index = 0; index < policies.Length; index++)
        {
            source.Append($"policy P{index}\n  require {policies[index].Condition}\n");
        }

        source.Append("module Portal\n  feature Reports\n");
        for (var index = 0; index < policies.Length; index++)
        {
            var (_, type, value, caller, allowed) = policies[index];
            var owner = $"\n          owner = {value}";
            var then = allowed ? $"then Filed{index}\n          for \"r-1\"\n          reportId = \"r-1\"" : "then denied";
            source.Append($$"""
                    slice StateChange File{{index}}
                      command File{{index}}
                        reportId String identifier
                        owner {{type}}
                        authorize P{{index}}
                        produces Filed{{index}}
                          for reportId
                          reportId = reportId
                      event Filed{{index}}
                        reportId String
                      specification Caller{{index}}
                        given caller{{caller}}
                        when File{{index}}
                          reportId = "r-1"{{owner}}
                        {{then}}

                """);
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("GuestNegation"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("guest-negation"), "guest-negation", "GuestNegation.play", source.ToString());
        var compiled = new SemanticModelCompiler().Compile("GuestNegation", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Location}: {diagnostic.Message}")));
        _plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
    }

    async Task Because()
    {
        var specifications = _plan.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications).ToArray();
        _vectors = specifications.Length;
        var report = await new SemanticSpecificationExecutor().Run(_plan, new([.. specifications.Select(specification => specification.Id)]), new());
        foreach (var specification in specifications)
        {
            var reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
            var stage = report.Results.Single(result => result.SpecificationId == specification.Id.ToString());
            if (!reference.Passed || stage.Outcome != SemanticSpecificationOutcome.Passed)
            {
                _mismatches.Add($"{specification.Name}: reference {reference.Execution.Kind} {string.Join(',', reference.Failures)}; Stage {stage.Outcome} {stage.Unsupported?.Details} {string.Join(',', stage.Failures)}");
            }
        }
    }

    [Fact] void should_run_every_guest_and_authenticated_vector() => _vectors.ShouldEqual(10);
    [Fact] void should_agree_with_the_reference_and_the_authored_outcome() => Assert.True(_mismatches.Count == 0, string.Join(Environment.NewLine, _mismatches));
}
#endif
