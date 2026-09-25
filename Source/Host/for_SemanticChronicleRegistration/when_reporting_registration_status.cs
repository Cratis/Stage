// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_reporting_registration_status : Specification
{
    StageStatus _loading = null!;
    StageStatus _refused = null!;

    void Because()
    {
        var services = Substitute.For<IServiceProvider>();
        var issues = new List<StageUnsupportedIssue>();
        _loading = SemanticHost.RegistrationStatus(null, issues, services, "Projects", "/tmp/stage-model");
        issues.Add(new("World", "model", "World rebuild refused"));
        _refused = SemanticHost.RegistrationStatus(null, issues, services, "Projects", "/tmp/stage-model");
    }

    [Fact] void should_report_loading_before_the_world_is_available() => _loading.State.ShouldEqual("loading");
    [Fact] void should_report_refusal_after_a_failed_rebuild() => _refused.State.ShouldEqual("unsupported");
}
