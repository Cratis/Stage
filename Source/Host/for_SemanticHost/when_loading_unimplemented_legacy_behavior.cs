// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_unimplemented_legacy_behavior : given.a_model_path
{
    [Theory]
    [InlineData("reaction batches atomic\n", "", "STAGE-ESM-043")]
    [InlineData("command reads protected\n", "", "STAGE-ESM-044")]
    [InlineData("", "        handler\n          implementation\n            hint \"Calculate the fee\"\n", "STAGE-IMPL-002")]
    public async Task should_refuse_before_registering_the_legacy_model(string boundary, string handler, string code)
    {
        await File.WriteAllTextAsync(_path, boundary + "module Billing\n  feature Invoices\n    slice StateChange ApplyFee\n      command ApplyFee\n" + handler + "        produces FeeApplied\n      event FeeApplied");
        var issues = new List<StageUnsupportedIssue>();
        var application = await EventModelHost.LoadModel(_path, issues);
        application.ShouldBeNull();
        var issue = Assert.Single(issues);
        issue.Capability.ShouldEqual("Plan");
        issue.Artifact.ShouldEqual("model");
        issue.Details.ShouldContain(code + ":");
    }
}
