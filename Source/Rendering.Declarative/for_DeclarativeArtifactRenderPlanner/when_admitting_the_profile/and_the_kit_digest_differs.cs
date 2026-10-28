// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_admitting_the_profile;

/// <summary>
/// A readable archive that is not the canonical one has a different digest than the loader computes.
/// </summary>
public class and_the_kit_digest_differs : a_refused_plan
{
    protected override string ExpectedCode => DeclarativeCodes.KitDigestMismatch;

    void Because() => _plan = Plan(Profile([.. Archive(), .. new byte[512]]));

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_only_the_expected_error() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([ExpectedCode]);
}
