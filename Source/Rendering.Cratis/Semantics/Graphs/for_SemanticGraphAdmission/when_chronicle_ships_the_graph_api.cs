// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphAdmission;

public class when_chronicle_ships_the_graph_api : Specification
{
    static readonly Version Release = new(19, 129, 0);

    [Fact] void should_not_admit_graphs_while_the_first_release_is_unset() => ChronicleGraphSupport.Admits("19.999.0", null).ShouldBeFalse();
    [Fact] void should_not_admit_the_release_before_it() => ChronicleGraphSupport.Admits("19.128.9", Release).ShouldBeFalse();
    [Fact] void should_admit_the_release_itself() => ChronicleGraphSupport.Admits("19.129.0", Release).ShouldBeTrue();
    [Fact] void should_admit_a_later_release() => ChronicleGraphSupport.Admits("20.0.1", Release).ShouldBeTrue();
    [Fact] void should_read_a_prerelease_as_its_release() => ChronicleGraphSupport.Admits("19.129.0-preview.2", Release).ShouldBeTrue();
    [Fact] void should_not_admit_a_version_it_cannot_read() => ChronicleGraphSupport.Admits("latest", Release).ShouldBeFalse();
    [Fact] void should_admit_the_pin_stage_has_today() => ChronicleGraphSupport.PinnedClientAdmits.ShouldBeTrue();
}
