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

public class with_checksum_concepts
{
    [Theory]
    [InlineData("luhn", "79927398713", true)]
    [InlineData("luhn", "79927398714", false)]
    [InlineData("luhn", "", false)]
    [InlineData("luhn", "abc0", true)]
    [InlineData("luhn", " 0", true)]
    [InlineData("luhn", "\t0", false)]
    [InlineData("luhn", "é0", false)]
    [InlineData("mod11", "123456785", true)]
    [InlineData("mod11", "123456784", false)]
    [InlineData("mod11", "60", false)]
    [InlineData("mod11", "", false)]
    [InlineData("mod11", "abc0", true)]
    [InlineData("mod97", "12345678978", true)]
    [InlineData("mod97", "12345678979", false)]
    [InlineData("mod97", "", false)]
    [InlineData("mod97", "8", false)]
    [InlineData("mod97", "abc98", true)]
    [InlineData("mod97", "ABC98", true)]
    [InlineData("mod97", " 98", true)]
    [InlineData("mod97", "98 ", false)]
    [InlineData("mod97", "\n98", false)]
    public async Task should_validate_the_same_checksum_as_the_reference(string algorithm, string value, bool valid)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Checksums"));
        var source = $"concept Reference : String\n  validate checksum {algorithm}\nmodule Shop\n  feature References\n    slice StateChange Check\n      command Check\n        value Reference\n        returns value";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("checksum"), "checksum", "Checksum.play", source);
        var compilation = new SemanticModelCompiler().Compile("Checksums", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        new SemanticRuntimeAdmission(plan).Blocking.ShouldBeEmpty();
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        using var runtime = (IDisposable)SemanticRuntimeHosting.Create(plan, appender);
        var result = await ((ISemanticRuntime)runtime).Execute(
            plan.Commands.Values.Single(),
            new Dictionary<string, JsonElement> { ["value"] = JsonSerializer.SerializeToElement(value) },
            new ClaimsPrincipal(),
            new(DateTimeOffset.UtcNow, "subject", "name", "user"),
            false);
        (result is SemanticAccepted).ShouldEqual(valid);
        if (!valid)
        {
            var rejected = (SemanticRejected)result;
            rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
            rejected.ValidationFailures.Single().Message.ShouldEqual($"A value must end in the exact '{algorithm}' check digits.");
        }
        await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
    }
}
