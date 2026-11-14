// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Xunit;

namespace Cratis.Stage.Host.for_EventModelHost;

public class when_loading_unenforced_concept_validation : Specification
{
    readonly List<StageUnsupportedIssue> _issues = [];
    StageApplication? _application;

    async Task Because() => _application = await EventModelHost.LoadModel("registry.play", _issues, _ =>
    {
        var model = EventModelLoader.LoadFromSource("""
            concept Name : String
              validate
                not empty
            module Registry
              feature Records
                slice StateChange Register
                  command RegisterRecord
                    name Name
                    produces RecordRegistered
                      name = name
                  event RecordRegistered
                    name Name
            """);
        return Task.FromResult(new StageApplication(model, null!));
    });

    [Fact] void should_refuse_the_application() => _application.ShouldBeNull();
    [Fact] void should_report_a_plan_issue() => Assert.Single(_issues).Capability.ShouldEqual("Plan");
    [Fact] void should_report_the_diagnostic() => Assert.Single(_issues).Details.ShouldContain("STAGE-ESM-081:");
    [Fact] void should_identify_the_declaration() => Assert.Single(_issues).Details.ShouldContain("concept 'Name' validation");
}
