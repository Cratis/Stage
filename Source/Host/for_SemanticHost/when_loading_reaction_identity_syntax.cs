// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_reaction_identity_syntax : given.a_model_path
{
    [Theory]
    [InlineData("runs as system", "STAGE-ESM-034")]
    [InlineData("from Account.Notes", "STAGE-ESM-035")]
    public async Task should_refuse_the_legacy_model_before_registration(string construct, string code)
    {
        await File.WriteAllTextAsync(_path, "eventsource Account\n  stream Notes\nmodule Work\n  feature Completion\n    slice Automation Complete\n      event Started\n      reaction Follow\n        " + construct + "\n        when Started");
        var issues = new List<StageUnsupportedIssue>();
        var application = await EventModelHost.LoadModel(_path, issues);
        application.ShouldBeNull();
        var issue = Assert.Single(issues);
        issue.Capability.ShouldEqual("Plan");
        issue.Artifact.ShouldEqual("model");
        issue.Details.StartsWith(code + ":", StringComparison.Ordinal).ShouldBeTrue();
    }
}
