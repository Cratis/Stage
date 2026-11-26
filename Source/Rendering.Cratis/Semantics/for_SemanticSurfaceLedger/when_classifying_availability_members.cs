// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSurfaceLedger;

public class when_classifying_availability_members : Specification
{
    [Fact]
    void should_keep_the_audience_name_rendered() =>
        SemanticSurfaceLedger.Entries["SemanticAudienceRequirement.Audience"].ShouldEqual(new(SemanticSurfaceDispositionKind.Rendered));
}
