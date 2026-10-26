// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_StructureConverter;

public class when_allocating_command_keys : Specification
{
    readonly string[][] _paths = [["M", "F", "S", "A"], ["M", "F", "S", "B"], ["M", "F", "T", "A"], ["M", "F", "T", "B"]];
    IReadOnlyDictionary<string, string> _keys = null!;

    void Because() => _keys = StructureConverter.CommandKeys(_paths);

    [Fact] void should_keep_both_commands_in_both_slices() => _keys.Values.Distinct(StringComparer.Ordinal).Count().ShouldEqual(4);
    [Fact] void should_encode_authored_segment_boundaries() => StructureConverter.CommandDescriptor(["a.b", "c"]).Equals(StructureConverter.CommandDescriptor(["a", "b.c"]), StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_preserve_authored_casing() => StructureConverter.CommandDescriptor(["A"]).Equals(StructureConverter.CommandDescriptor(["a"]), StringComparison.Ordinal).ShouldBeFalse();
    [Fact]
    void should_not_conflict_with_an_authored_bare_key()
    {
        var bare = StructureConverter.CommandDescriptor(_paths[0]);
        var keys = StructureConverter.CommandKeys([.. _paths, ["Other", bare]]);
        keys.Values.Distinct(StringComparer.Ordinal).Count().ShouldEqual(5);
        keys[StructureConverter.CommandDescriptor(["Other", bare])].ShouldEqual(bare);
    }
    [Fact] void should_be_independent_of_enumeration_order() => StructureConverter.CommandKeys(_paths.Reverse()).OrderBy(pair => pair.Key, StringComparer.Ordinal).ShouldEqual(_keys.OrderBy(pair => pair.Key, StringComparer.Ordinal));
}
