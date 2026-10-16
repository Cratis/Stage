// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Host.for_EventModelHost;

public class when_loading_an_unknown_validation_rule : Specification
{
    readonly List<StageUnsupportedIssue> _issues = [];
    StageApplication? _application;

    async Task Because() => _application = await EventModelHost.LoadModel("model.play", _issues, _ =>
    {
        // Supply syntax the current compiler cannot author, through the real converter and catch boundary.
        var rule = new ValidationRuleSyntax("reference", (ValidationRuleKind)999, null, null, SourceLocation.Start);
        ValidationRuleConverter.Convert([new DeclarativeValidateSyntax([rule], SourceLocation.Start)]);
        return Task.FromResult<StageApplication>(null!);
    });

    [Fact] void should_refuse_registration() => _application.ShouldBeNull();
    [Fact] void should_report_a_plan_issue() => Assert.Single(_issues).Capability.ShouldEqual("Plan");
    [Fact] void should_identify_the_model() => Assert.Single(_issues).Artifact.ShouldEqual("model");
    [Fact] void should_report_the_code() => Assert.Single(_issues).Details.ShouldContain("STAGE-ESM-005");
    [Fact] void should_report_the_kind() => Assert.Single(_issues).Details.ShouldContain("999");
    [Fact] void should_report_the_property() => Assert.Single(_issues).Details.ShouldContain("reference");
}
