// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_direct_productions : given.an_application_trigger_reaction
{
    void Because() => Inspect();

    [Fact] void should_admit_the_named_trigger_payload_mapping() => _diagnostics.ShouldBeEmpty();
}
