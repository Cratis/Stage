// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Evaluates generated Arc policies that use `not` against Screenplay's reference evaluator and the authored outcome
/// table: an omitted claim target is unknown, negation keeps it unknown, and an unknown policy result denies.
/// </summary>
public class when_rendering_policy_negation : Specification
{
    // Condition, then the allowed outcome per role (none, A, Service) and owner value (omitted, "person" = the caller's claim, "other").
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

    static readonly string?[] _owners = [null, "person", "other"];
    static readonly string[][] _roles = [[], ["A"], ["Service"]];

    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;
    string _generatedPolicies = null!;
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
            source.Append($$"""
                    slice StateChange File{{policy}}
                      command File{{policy}}
                        reportId String identifier
                        owner String optional
                        authorize P{{policy}}
                        produces Filed{{policy}}
                          for reportId
                          reportId = reportId
                      event Filed{{policy}}
                        reportId String

                """);
        }

        _model = invoice_model.Compile(source.ToString());
    }

    void Because()
    {
        _plan = invoice_model.Plan(_model);
        if (!_plan.Success) return;
        var sources = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
        _generatedPolicies = string.Join('\n', sources.Where(file => file.RelativePath.StartsWith("GeneratedPolicies/StagePolicy_", StringComparison.Ordinal)).Select(file => file.Content));
        var assembly = RenderedOutput.Load(sources);
        var executionPlan = SemanticExecutionPlan.Compile(_model).Plan!;
        var commands = _model.Application.Modules.Single().Features.Single().Slices.Select(slice => slice.Commands.Single()).ToArray();
        for (var policy = 0; policy < _policies.Length; policy++)
        {
            var command = commands.Single(candidate => candidate.Name == $"File{policy}");
            var generated = (IAuthorizationPolicy)Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == SemanticPolicyArtifactRenderer.Name(command.Id)))!;
            var type = assembly.GetTypes().Single(candidate => candidate.Name == command.Name);
            var allowed = _policies[policy].Allowed.Split(' ');
            for (var role = 0; role < _roles.Length; role++)
            {
                for (var owner = 0; owner < _owners.Length; owner++)
                {
                    _vectors++;
                    var expected = allowed[role][owner] == 'Y';
                    var instance = Create(type, _owners[owner]);
                    var resource = new CommandContext(CorrelationId.New(), type, instance, [], CommandContextValues.Empty);
                    var principal = new ClaimsPrincipal(new ClaimsIdentity(
                        [.. _roles[role].Select(name => new Claim(ClaimTypes.Role, name)), new Claim("owner", "person")], "fixture"));
                    var rendered = generated.IsAuthorized(new(principal, type, resource), CancellationToken.None).AsTask().GetAwaiter().GetResult();
                    var reference = Reference(executionPlan, command, _roles[role], _owners[owner]);
                    if (rendered != expected || reference != expected)
                    {
                        _mismatches.Add($"{_policies[policy].Condition} roles [{string.Join(',', _roles[role])}] owner {_owners[owner] ?? "omitted"}: expected {expected}, generated {rendered}, reference {reference}");
                    }
                }
            }
        }
    }

    [Fact] void should_render_the_negated_policies() => _plan.Success.ShouldBeTrue();
    [Fact] void should_evaluate_every_vector() => _vectors.ShouldEqual(_policies.Length * _roles.Length * _owners.Length);
    [Fact] void should_agree_with_the_reference_and_the_authored_outcome() => Assert.True(_mismatches.Count == 0, string.Join(Environment.NewLine, _mismatches));
    [Fact] void should_combine_unknown_explicitly() => _generatedPolicies.Contains("PolicyValues.Not(PolicyValues.Truth(", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_allow_only_a_definite_true() => _generatedPolicies.Contains(") == true)", StringComparison.Ordinal).ShouldBeTrue();

    static object Create(Type type, string? owner)
    {
        var constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().Length == 2);
        return constructor.Invoke([.. constructor.GetParameters().Select(parameter => string.Equals(parameter.Name, "reportId", StringComparison.OrdinalIgnoreCase) ? (object?)"r-1" : owner)]);
    }

    static bool Reference(SemanticExecutionPlan plan, SemanticCommand command, string[] roles, string? owner)
    {
        var values = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
        values.Add(new(command.Properties.Single(property => property.Name == "reportId").Id, SemanticValue.Text("r-1")));
        if (owner is not null)
        {
            values.Add(new(command.Properties.Single(property => property.Name == "owner").Id, SemanticValue.Text(owner)));
        }

        var request = SemanticExecutionRequest.Create(command.Id, values.ToImmutable(), []) with
        {
            Caller = new(true, [.. roles], [new("owner", "person")])
        };
        return new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request) is not SemanticRejected { Category: SemanticRejectionCategory.Unauthorized };
    }
}
#endif
