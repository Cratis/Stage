// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

/// <summary>
/// The live runtime authorizes through Screenplay's three-valued evaluator: an omitted or non-text claim target is
/// unknown, `not` keeps it unknown, and an unknown result denies.
/// </summary>
public class with_a_negated_policy : Specification
{
    ISemanticRuntime _runtime = null!;
    SemanticExecutionPlan _plan = null!;
    readonly Dictionary<string, SemanticExecutionResult> _results = [];

    void Establish()
    {
        _plan = compiled_plan.From("""
            policy NotOwner
              require not claim "owner" matches owner
            module Reports
              feature Filing
                slice StateChange FileReport
                  command FileReport
                    reportId Uuid identifier
                    owner String optional
                    authorize NotOwner
                    produces ReportFiled
                      for reportId
                      owner = owner
                  event ReportFiled
                    owner String optional
                slice StateChange CountReport
                  command CountReport
                    reportId Uuid identifier
                    owner Int optional
                    authorize NotOwner
                    produces ReportCounted
                      for reportId
                      owner = owner
                  event ReportCounted
                    owner Int optional
            """);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        _runtime = SemanticRuntimeHosting.Create(_plan, appender);
    }

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("owner", "person")], "fixture"));
        foreach (var (name, command, owner) in new (string, string, object?)[]
        {
            ("omitted", "FileReport", null), ("person", "FileReport", "person"), ("other", "FileReport", "other"), ("number", "CountReport", 42)
        })
        {
            var payload = new Dictionary<string, JsonElement> { ["reportId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6") };
            if (owner is not null)
            {
                payload["owner"] = JsonSerializer.SerializeToElement(owner);
            }

            _results[name] = await _runtime.Execute(_plan.Commands.Values.Single(candidate => candidate.Name == command), payload, principal, new(DateTimeOffset.UtcNow, "subject", "name", "user"), false);
        }
    }

    [Fact] void should_compile_an_esm_v7_model() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
    [Fact] void should_admit_the_negated_policy() => new SemanticRuntimeAdmission(_plan).Blocking.ShouldBeEmpty();
    [Fact] void should_deny_an_omitted_target() => Denied("omitted").ShouldBeTrue();
    [Fact] void should_deny_a_matching_claim() => Denied("person").ShouldBeTrue();
    [Fact] void should_deny_a_non_text_target() => Denied("number").ShouldBeTrue();
    [Fact] void should_allow_a_different_owner() => (_results["other"] is SemanticAccepted).ShouldBeTrue();

    bool Denied(string name) => _results[name] is SemanticRejected { Category: SemanticRejectionCategory.Unauthorized };
}
