// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_inspecting;

public class with_composite_read_model_keys : Specification
{
    WebApplication _app = null!;
    HttpClient _client = null!;
    SemanticId _readModel;
    JsonElement _instances;

    async Task Establish()
    {
        // The legacy engine refuses composite read-model keys, so this browser runs on the semantic runtime alone.
        var plan = compiled_plan.From(composite_key_payments.Source);
        _readModel = plan.ReadModels.Values.Single().Id;
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        var facts = new List<SemanticFact>();
        ((ISemanticFactTail)appender).Tail().Returns(_ => facts.Count == 0 ? ulong.MaxValue : (ulong)facts.Count - 1);
        appender.Append(Arg.Do<IReadOnlyList<SemanticFact>>(facts.AddRange), Arg.Any<SemanticCommandOccurrence>()).Returns(Task.CompletedTask);
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        var command = plan.Commands.Values.Single(candidate => candidate.Name == "ReceivePayment");
        foreach (var (paymentId, method) in new[] { ("payment-1", "card"), ("payment-2", "cash") })
        {
            var outcome = await runtime.Execute(
                command,
                new Dictionary<string, JsonElement>
                {
                    ["paymentId"] = JsonSerializer.SerializeToElement(paymentId),
                    ["invoice"] = JsonSerializer.SerializeToElement("INV-1"),
                    ["method"] = JsonSerializer.SerializeToElement(method),
                    ["amount"] = JsonSerializer.SerializeToElement(5)
                },
                new ClaimsPrincipal(),
                new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"),
                false);
            outcome.ShouldBeOfExactType<SemanticAccepted>();
        }

        _app = WebApplication.CreateBuilder().Build();
        SemanticReadModelBrowser.Map(_app, runtime, () => true);
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
    }

    async Task Because()
    {
        using var response = await _client.GetAsync($"{SemanticReadModelBrowser.Route}/{_readModel}");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _instances = document.RootElement.GetProperty("instances").Clone();
    }

    [Fact] void should_report_each_key_as_an_object_of_its_parts() => _instances.EnumerateArray().All(instance => instance.GetProperty("key").ValueKind == JsonValueKind.Object).ShouldBeTrue();
    [Fact] void should_name_every_part() => _instances[0].GetProperty("key").EnumerateObject().Select(part => part.Name).ShouldContainOnly("invoiceId", "method");
    [Fact] void should_keep_instances_that_differ_in_one_part_apart() => _instances.EnumerateArray().Select(instance => instance.GetProperty("key").GetProperty("method").GetString()).ShouldContainOnly("card", "cash");

    async Task Destroy()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }
}
