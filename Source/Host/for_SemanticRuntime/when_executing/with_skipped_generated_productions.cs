// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_skipped_generated_productions : Specification
{
    ISemanticRuntime _runtime = null!;
    IAppendSemanticFacts _appender = null!;
    SemanticAccepted _result = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish()
    {
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(GeneratedValuesCorpus.V15.EsmBytes.AsSpan())).Plan!;
        _appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)_appender).Tail().Returns(ulong.MaxValue);
        _admission = new(plan);
        _runtime = SemanticRuntimeHosting.Create(plan, _appender);
    }

    async Task Because() => _result = (SemanticAccepted)await _runtime.Execute(
        _runtime.Plan.Commands.Values.Single(command => command.Name == "Create"),
        new Dictionary<string, JsonElement> { ["enabled"] = JsonSerializer.SerializeToElement(false) },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UtcNow, "subject", "name", "user"),
        false);

    void Destroy() => ((IDisposable)_runtime).Dispose();

    [Fact] void should_admit_all_canonical_specifications() => _admission.Entries.Where(entry => entry.Kind == "specification").Select(entry => entry.Status).Distinct().ShouldContainOnly("supported");
    [Fact] void should_return_the_generated_receipt() => ((SemanticTextValue)((SemanticScalarExecutionResponse)_result.Response!).Value).Value.Length.ShouldEqual(21);
    [Fact] void should_produce_no_facts() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_not_append() => _appender.DidNotReceiveWithAnyArgs().Append(default!, default!);
}
