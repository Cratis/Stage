// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerInspection;

public class when_parsing_a_found_container : Specification
{
    static readonly string _id = new('a', 64);
    GeneratedApplicationContainerInspection _inspection = null!;

    void Because() => _inspection = GeneratedApplicationContainerInspection.Parse(new(0, $"{_id}\n{{\"a\":\"1\"}}\n", string.Empty), new(_id));

    [Fact] void should_find_it() => _inspection.Kind.ShouldEqual(GeneratedApplicationContainerInspectionKind.Found);
    [Fact] void should_read_the_identifier() => _inspection.Id!.Value.ShouldEqual(_id);
    [Fact] void should_read_the_labels() => _inspection.Labels["a"].ShouldEqual("1");
}
