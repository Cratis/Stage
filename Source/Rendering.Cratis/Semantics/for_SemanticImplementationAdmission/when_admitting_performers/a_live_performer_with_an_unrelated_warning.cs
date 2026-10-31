// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_admitting_performers;

public class a_live_performer_with_an_unrelated_warning : given.a_performer
{
    void Establish() => PerformerBody = "int unused = 0; return readModels;";

    Task Because() => Prepare();

    [Fact] void should_reject_the_warning() => _admission.Reason.ShouldContain("CS0219");
    [Fact] void should_not_hide_it_as_a_reference_warning() => _admission.Reason.ShouldNotContain("CS1701");
}
