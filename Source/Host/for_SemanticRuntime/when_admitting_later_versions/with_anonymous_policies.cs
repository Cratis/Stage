// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_anonymous_policies : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_run_the_corpus_command_for_any_caller(bool authenticated)
    {
        var model = SemanticModelSerializer.Deserialize(AnonymousPoliciesCorpus.V31.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        var admission = new SemanticRuntimeAdmission(plan);
        admission.Blocking.ShouldBeEmpty();
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        using var lifetime = (IDisposable)runtime;
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], authenticated ? "spec" : null));
        var specification = plan.Specifications.Values.First(specification => specification.When is not null);
        var command = plan.Commands[specification.When.Command];
        var payload = specification.When.Values.ToDictionary(
            value => command.Properties.Single(property => property.Id == value.TargetProperty).Name,
            value => JsonSerializer.SerializeToElement(SemanticJsonValues.ToObject(value.Value)));
        var outcome = await runtime.Execute(command, payload, principal, new(DateTimeOffset.UnixEpoch, "subject", "name", "user"), false);
        Assert.IsType<SemanticAccepted>(outcome).Facts.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_query_an_anonymous_read_model_for_any_caller(bool authenticated)
    {
        var plan = compiled_plan.From("""
            policy Public
              require anonymous
            module M
              feature F
                slice StateView Lookup
                  readmodel Entry
                    id String key
                  query Find => Entry?
                    by id String
                    authorize Public
            """);
        new SemanticRuntimeAdmission(plan).Blocking.ShouldBeEmpty();
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        using var lifetime = (IDisposable)runtime;
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], authenticated ? "spec" : null));
        var result = await runtime.Query(plan.Queries.Values.Single(), SemanticValue.Text("entry"), principal);
        Assert.IsType<SemanticAccepted>(result);
    }
}
