// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerInspection;

public class when_parsing_a_missing_container : Specification
{
    GeneratedApplicationContainerInspection _inspection = null!;

    void Because() => _inspection = GeneratedApplicationContainerInspection.Parse(new(1, string.Empty, "Error: No such object: abc"));

    [Fact] void should_report_it_missing() => _inspection.Kind.ShouldEqual(GeneratedApplicationContainerInspectionKind.Missing);
}
