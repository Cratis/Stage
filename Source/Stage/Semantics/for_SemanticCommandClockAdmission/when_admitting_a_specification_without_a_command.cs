// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandClockAdmission;

public class when_admitting_a_specification_without_a_command
{
    [Fact]
    public void should_not_refuse_clock_members_belonging_to_other_commands()
    {
        const string source = """
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at DateTime
                    validate
                      at > now
                    produces Kept
                      for id
                  event Kept
                  specification Original
                    when Keep
                      id = "window"
                      at = "2026-01-02T00:00:00.0000000Z"
                    then Kept
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("NoCommandClock"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("NoCommandClock", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan!;
        var specification = plan.Specifications.Values.Single() with { When = null };
        plan.SliceOf(specification.Id).ShouldNotBeNull();
        SemanticRunAdmission.RunStanding(plan.Model, new HashSet<SemanticId>()).UnknownMembers.ShouldNotBeEmpty();
        SemanticRunAdmission.CommandRangeRefusal(plan, plan.Commands.Values.Single()).ShouldNotBeNull();
        SemanticRunAdmission.Check(plan, specification).ShouldBeNull();
    }
}
