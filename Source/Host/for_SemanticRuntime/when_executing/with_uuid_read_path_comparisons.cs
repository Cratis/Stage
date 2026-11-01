// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

[PerFactSpecificationLifecycle]
public class with_uuid_read_path_comparisons : Specification
{
    const string Granted = "a1111111-1111-4111-8111-111111111111";
    const string Source = """
        command reads protected
        concept MemberId : Uuid
        module Plants
          feature Access
            slice StateChange Watering
              event AccessGranted
                memberId MemberId
              event AccessRevoked
              event PlantWatered
              command GrantAccess
                id String identifier
                memberId MemberId
                produces AccessGranted
                  for id
                  memberId = memberId
              command RevokeAccess
                id String identifier
                produces AccessRevoked
                  for id
              command WaterPlant
                id String identifier
                grantId String
                requestedBy MemberId
                reads PlantAccessGrant by grantId
                validate
                  require PlantAccessGrant.memberId == requestedBy
                    message "Only the granted member may water the plant."
                produces PlantWatered
                  for id
            slice StateView Grants
              readmodel PlantAccessGrant
                id String key
                memberId MemberId
              projection BuildGrant => PlantAccessGrant
                from AccessGranted
                  id = $eventSourceId
                  memberId = memberId
                remove with AccessRevoked
              query GrantById => PlantAccessGrant optional
                by id String
        """;

    SemanticExecutionPlan _plan = null!;
    ISemanticRuntime _runtime = null!;
    IAppendSemanticFacts _appender = null!;
    ulong _tail;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("UuidReadPaths"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("UuidReadPaths", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan;
        _appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        _tail = ulong.MaxValue;
        ((ISemanticFactTail)_appender).Tail().Returns(_ => _tail);
        _appender.Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>()).Returns(call =>
        {
            var count = (ulong)call.Arg<IReadOnlyList<SemanticFact>>().Count;
            _tail = _tail == ulong.MaxValue ? count - 1 : _tail + count;
            return Task.CompletedTask;
        });
        _runtime = SemanticRuntimeHosting.Create(_plan, _appender);
    }

    void Destroy() => ((IDisposable)_runtime).Dispose();

    [Fact]
    public async Task should_accept_the_granted_member()
    {
        await Grant();
        var accepted = Assert.IsType<SemanticAccepted>(await Water(Granted));
        accepted.World.Facts.Length.ShouldEqual(2);
    }

    [Fact]
    public async Task should_reject_another_member()
    {
        await Grant();
        var rejected = Assert.IsType<SemanticRejected>(await Water("b2222222-2222-4222-8222-222222222222"));
        rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
        rejected.Details.ShouldEqual("Only the granted member may water the plant.");
        rejected.World.Facts.Length.ShouldEqual(1);
    }

    [Fact]
    public async Task should_reject_a_non_canonical_member_before_the_requirement_runs()
    {
        await Grant();
        var rejected = Assert.IsType<SemanticRejected>(await Water(Granted.ToUpperInvariant()));
        rejected.Details.ShouldNotEqual("Only the granted member may water the plant.");
        rejected.World.Facts.Length.ShouldEqual(1);
    }

    async Task Grant() => Assert.IsType<SemanticAccepted>(await Execute("GrantAccess", new { id = "grant", memberId = Granted }));

    Task<SemanticExecutionResult> Water(string requestedBy) => Execute("WaterPlant", new { id = "watering", grantId = "grant", requestedBy });

    Task<SemanticExecutionResult> Execute(string name, object payload)
    {
        var command = _plan.Commands.Values.Single(command => command.Name == name);
        var json = JsonSerializer.SerializeToElement(payload);
        var values = command.Properties.Where(property => !property.IsGenerated && json.TryGetProperty(property.Name, out _))
            .ToDictionary(property => property.Name, property => json.GetProperty(property.Name));

        return _runtime.Execute(command, values, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"), false);
    }
}
