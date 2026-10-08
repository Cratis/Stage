// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

/// <summary>
/// Pins Screenplay 4.81's three-valued policy negation: a claim comparison with a non-text target is unknown,
/// negation keeps it unknown, and an unknown policy result denies. Every vector runs through the reference runner
/// and the Stage executor, so both must agree with the authored outcome.
/// </summary>
public class with_negated_policies : Specification
{
    // Condition, then the allowed outcome per owner value (42 = not text, "person" = the caller's claim, "other") and role (none, A, Service).
    static readonly (string Condition, string Allowed)[] _policies =
    [
        ("not claim \"owner\" matches owner", "--Y --Y --Y"),
        ("not not claim \"owner\" matches owner", "-Y- -Y- -Y-"),
        ("not (role \"A\" or claim \"owner\" matches owner)", "--Y --- --Y"),
        ("role \"A\" or not claim \"owner\" matches owner", "--Y YYY --Y"),
        ("not claim \"owner\" matches owner or role \"A\"", "--Y YYY --Y"),
        ("not (authenticated and claim \"owner\" matches owner)", "--Y --Y --Y"),
        ("not (role \"A\" and claim \"owner\" matches owner)", "YYY --Y YYY"),
        ("not (claim \"owner\" matches owner and claim \"owner\" matches subject)", "YYY YYY YYY"),
        ("not (claim \"owner\" matches owner or claim \"owner\" matches subject)", "--Y --Y --Y"),
        ("authenticated and not role \"Service\"", "YYY YYY ---")
    ];

    static readonly string[] _owners = ["42", "\"person\"", "\"other\""];
    static readonly string[] _roles = [string.Empty, "A", "Service"];

    SemanticExecutionPlan _plan = null!;
    readonly List<string> _mismatches = [];
    int _vectors;

    void Establish()
    {
        var source = new StringBuilder();
        for (var policy = 0; policy < _policies.Length; policy++)
        {
            source.Append($"policy P{policy}\n  require {_policies[policy].Condition}\n");
        }

        source.Append("module Portal\n  feature Reports\n");
        for (var policy = 0; policy < _policies.Length; policy++)
        {
            var allowed = _policies[policy].Allowed.Split(' ');
            (string Command, string Type, int[] Owners)[] commands = [($"File{policy}", "String", [1, 2]), ($"Count{policy}", "Int", [0])];
            foreach (var (command, type, owners) in commands)
            {
                source.Append($$"""
                        slice StateChange {{command}}
                          command {{command}}
                            reportId String identifier
                            owner {{type}} optional
                            authorize P{{policy}}
                            produces {{command}}Recorded
                              for reportId
                              owner = owner
                          event {{command}}Recorded
                            owner {{type}} optional

                    """);
                for (var role = 0; role < _roles.Length; role++)
                {
                    foreach (var owner in owners)
                    {
                        var roleLine = _roles[role].Length == 0 ? string.Empty : $"\n          role \"{_roles[role]}\"";
                        var then = allowed[role][owner] == 'Y' ? $"then {command}Recorded\n          for \"r-1\"\n          owner = {_owners[owner]}" : "then denied";
                        source.Append($$"""
                                  specification P{{policy}}Role{{role}}Owner{{owner}}
                                    given caller
                                      authenticated{{roleLine}}
                                      claim "owner" = "person"
                                    when {{command}}
                                      reportId = "r-1"
                                      owner = {{_owners[owner]}}
                                    {{then}}

                            """);
                    }
                }
            }
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Negation"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("negation"), "negation", "Negation.play", source.ToString());
        var compiled = new SemanticModelCompiler().Compile("Negation", SemanticDocumentSet.Create([document], catalog));
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

    [Fact] void should_run_every_vector() => _vectors.ShouldEqual(_policies.Length * _roles.Length * _owners.Length);
    [Fact] void should_agree_with_the_reference_and_the_authored_outcome() => Assert.True(_mismatches.Count == 0, string.Join(Environment.NewLine, _mismatches));
}
#endif
