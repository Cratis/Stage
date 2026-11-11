// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_forwarding_a_body_worker_profile_version : Specification
{
    ProcessStartInfo _with = null!;
    ProcessStartInfo _without = null!;

    void Establish()
    {
        _with = new();
        _without = new();
    }

    void Because()
    {
        SemanticBodyWorkerHosting.AddBodyWorkerArguments(_with, new(true, "worker.dll") { ProfileVersion = "4" });
        SemanticBodyWorkerHosting.AddBodyWorkerArguments(_without, new(true, "worker.dll"));
    }

    [Fact] void should_forward_the_profile_version_when_set() => _with.ArgumentList.TakeLast(2).ShouldEqual(["--body-worker-profile-version", "4"]);
    [Fact] void should_not_forward_a_profile_version_when_unset() => _without.ArgumentList.ShouldNotContain("--body-worker-profile-version");
}
