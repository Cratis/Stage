// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_a_command_reads_a_passive_read_model : Specification
{
    [Theory]
    [InlineData(false, "original")]
    [InlineData(true, "original")]
    [InlineData(false, "")]
    [InlineData(true, "")]
    public async Task should_make_the_same_decision_from_the_requested_keys_events(bool passive, string value)
    {
        var form = ProtectedCommandReadsCorpus.SourceForms.Single(form => form.Name == "single");
        var source = Encoding.UTF8.GetString(form.Documents.Single().Bytes.AsSpan())
            .Replace("      query CurrentById => Current optional\n        by id String\n", string.Empty, StringComparison.Ordinal);
        if (passive) source = source.Replace("\n      readmodel Current\n", "\n      readmodel Current\n        passive\n", StringComparison.Ordinal);
        const string application = "PassiveReads";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(application));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("passive"), "passive", "Passive.play", source);
        var compiled = new SemanticModelCompiler().Compile(application, SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics));
        compiled.Value!.PassiveReadModels.Count.ShouldEqual(passive ? 1 : 0);
        var plan = SemanticExecutionPlan.Compile(compiled.Value.Model).Plan!;
        var log = new a_semantic_fact_log();
        var runtime = log.Runtime(plan, SemanticWorld.Empty);
        using var lifetime = (IDisposable)runtime;
        Assert.IsType<SemanticAccepted>(await Execute("Store", new { id = "reference", value }));
        Assert.IsType<SemanticAccepted>(await Execute("Store", new { id = "other", value = "unrelated" }));
        var result = await Execute("Consume", new { id = "target", sourceId = "reference" });
        if (value.Length == 0)
        {
            Assert.IsType<SemanticRejected>(result).Details.ShouldEqual("The stored value must not be empty.");
        }
        else
        {
            Assert.IsType<SemanticAccepted>(result).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("original"));
        }

        Task<SemanticExecutionResult> Execute(string name, object payload)
        {
            var command = plan.Commands.Values.Single(command => command.Name == name);
            var json = JsonSerializer.SerializeToElement(payload);
            var values = command.Properties.Where(property => json.TryGetProperty(property.Name, out _)).ToDictionary(property => property.Name, property => json.GetProperty(property.Name));
            return runtime.Execute(command, values, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"), validateOnly: false);
        }
    }
}
