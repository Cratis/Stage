// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_admitting_performers;

public class a_live_collection_performer_over_a_projected_read_model : given.a_performer
{
    Task Because() => Prepare();

    [Fact] void should_admit_the_body() => _admission.Body.ShouldEqual("return readModels;");
    [Fact] void should_have_no_gap_reason() => Assert.True(_admission.Reason is null, _admission.Reason);
}
