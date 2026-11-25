// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticCommandClock;

public class when_executing_property_validations : Specification
{
    const string Source = """
        module Scheduling
          feature Windows
            slice StateChange Keep
              command Keep
                id String identifier
                at DateTime
                validate
                  at >= now
                produces Kept
                  for id
                  recordedAt = $context.occurred
              command Unsupported
                id String identifier
                at Date
                validate
                  at >= today
                produces Kept
                  for id
                  recordedAt = $context.occurred
              command Plain
                id String identifier
                produces Kept
                  for id
                  recordedAt = $context.occurred
              event Kept
                recordedAt DateTime
        """;

    SemanticExecutionPlan _plan = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RuntimeClockValidation"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("RuntimeClockValidation", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model);
        Assert.True(plan.Success, string.Join(';', plan.Issues));
        _plan = plan.Plan!;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task should_use_the_supplied_instant_for_validation_and_the_event(int difference)
    {
        var instant = new DateTimeOffset(2026, 1, 2, 2, 0, 0, TimeSpan.FromHours(2)).AddTicks(1);
        var occurrence = new SemanticCommandOccurrence(instant, "subject", "Name", "user");
        var runtime = SemanticRuntimeHosting.Create(_plan, Substitute.For<IAppendSemanticFacts>());
        using var lifetime = (IDisposable)runtime;
        var properties = new Dictionary<string, JsonElement>
        {
            ["id"] = JsonSerializer.SerializeToElement("window"),
            ["at"] = JsonSerializer.SerializeToElement(instant.UtcDateTime.AddTicks(difference).ToString("O", CultureInfo.InvariantCulture))
        };
        var result = await runtime.Execute(Command("Keep"), properties, new ClaimsPrincipal(), occurrence, true);
        if (difference == 0)
        {
            Assert.IsType<SemanticAccepted>(result).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text(instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)));
        }
        else
        {
            var rejected = Assert.IsType<SemanticRejected>(result);
            rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
            rejected.ValidationFailures.Single().Message.ShouldEqual($"A value must be at least '{instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)}'.");
        }
        var admission = new SemanticRuntimeAdmission(_plan);
        admission.Blocking.ShouldBeEmpty();
        admission.Entries.Single(entry => entry.Artifact == Command("Keep").Id.ToString()).Status.ShouldEqual("supported");
        admission.Entries.Single(entry => entry.Artifact == Command("Unsupported").Id.ToString()).Status.ShouldEqual("unsupported");
        properties["at"] = JsonSerializer.SerializeToElement("2026-01-02");
        Assert.IsType<SemanticUnsupported>(await runtime.Execute(Command("Unsupported"), properties, new ClaimsPrincipal(), occurrence, true)).Details.ShouldContain("STAGE-ESM-016:");
        properties.Remove("at");
        Assert.IsType<SemanticAccepted>(await runtime.Execute(Command("Plain"), properties, new ClaimsPrincipal(), occurrence, true));
    }

    SemanticCommand Command(string name) => _plan.Commands.Values.Single(command => command.Name == name);
}
