// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_admitting_performers;

public class a_live_performer_over_an_unbuilt_read_model : given.a_performer
{
    Task Because() => Prepare(built: false);

    [Fact] void should_explain_the_unobservable_model() => _admission.Reason.ShouldContain("cannot be observed");
    [Fact] void should_name_the_model() => _admission.Reason.ShouldContain("PersonSummary");
}
