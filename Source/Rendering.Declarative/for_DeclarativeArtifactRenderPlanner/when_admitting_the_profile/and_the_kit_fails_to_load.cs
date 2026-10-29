// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Kits;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_admitting_the_profile;

public class and_the_kit_fails_to_load : a_refused_plan
{
    protected override string ExpectedCode => KitCodes.MissingManifest;

    void Because() => _plan = Plan(Profile(Archive(new Dictionary<string, System.Collections.Immutable.ImmutableArray<byte>>(StringComparer.Ordinal) { ["templates/index.sbn"] = Bytes("index") })));

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_only_the_expected_error() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([ExpectedCode]);
}
