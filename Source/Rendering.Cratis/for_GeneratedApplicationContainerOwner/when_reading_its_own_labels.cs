// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerOwner;

public class when_reading_its_own_labels : Specification
{
    GeneratedApplicationContainerOwner _owner = null!;
    GeneratedApplicationContainerOwner _read = null!;
    bool _read_ok;

    void Establish() => _owner = new() { Host = "host-a", ProcessId = 4242, ProcessStartedUnixSeconds = 1_000_000, RunToken = "0123abcd", StartedUnixSeconds = 2_000_000 };

    void Because() => _read_ok = GeneratedApplicationContainerOwner.TryRead(Labels(_owner), out _read);

    [Fact] void should_read_them() => _read_ok.ShouldBeTrue();
    [Fact] void should_produce_an_equal_owner() => _read.ShouldEqual(_owner);
    [Fact] void should_write_six_labels() => _owner.Labels().Count(argument => argument == "--label").ShouldEqual(6);

    internal static Dictionary<string, string> Labels(GeneratedApplicationContainerOwner owner) =>
        owner.Labels().Chunk(2).Select(pair => pair[1].Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);
}
