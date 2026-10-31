// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_admitting_performers;

public class a_snapshot_performer_with_live_versions : given.a_performer
{
    Task Because() => Prepare(live: false, resultVersion: 2);

    [Fact] void should_name_the_actual_pair() => _admission.Reason.ShouldContain("version 1/2");
    [Fact] void should_name_the_expected_pair() => _admission.Reason.ShouldContain("renders 1/1");
}
