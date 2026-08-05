// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ApplicationSet.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ApplicationSet;

public class when_finding_identifier_concepts : an_application_with_a_nested_feature
{
    [Fact] void should_mark_the_concept_used_as_an_identifier() => _applicationSet.IdentifierConceptNames.ShouldContainOnly("InvoiceId");
}
