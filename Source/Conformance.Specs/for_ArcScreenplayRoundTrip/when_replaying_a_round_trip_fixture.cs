// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

// The opt-in replay Arc uses to reproduce one fixture's recovery: CRATIS_STAGE_ARC_REPLAY_FIXTURES (comma separated fixture names) and
// CRATIS_STAGE_ARC_REPLAY_DIRECTORY select and receive it. Without them the replay does nothing, and it never replaces the full gate.
public class when_replaying_a_round_trip_fixture : IDisposable
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"stage-arc-replay-{Guid.NewGuid():N}");

    [Fact]
    void should_keep_the_emitted_source_the_recovered_source_and_the_observation()
    {
        var replayed = round_trip_replay.Replay([round_trip_fixtures.RoutedCommand], _directory);
        var directory = Path.Combine(_directory, round_trip_fixtures.RoutedCommand);

        Assert.Equal([round_trip_fixtures.RoutedCommand], replayed);
        Assert.NotEmpty(Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(directory, "Recovered.play")));
        Assert.True(File.Exists(Path.Combine(directory, "observation.json")));
    }

    [Fact]
    void should_replay_only_the_named_fixture() =>
        Assert.Equal([round_trip_fixtures.FilteredReactor], round_trip_replay.Replay([round_trip_fixtures.FilteredReactor], _directory));

    [Fact]
    void should_refuse_a_fixture_that_is_not_eligible() =>
        Assert.ThrowsAny<Exception>(() => round_trip_replay.Replay(["No.Such.Fixture"], _directory));

    [Fact]
    void should_replay_the_fixtures_requested_through_the_environment()
    {
        var requested = Environment.GetEnvironmentVariable(round_trip_replay.FixturesVariable);
        var directory = Environment.GetEnvironmentVariable(round_trip_replay.DirectoryVariable);
        if (string.IsNullOrWhiteSpace(requested) || string.IsNullOrWhiteSpace(directory)) return;

        var names = requested.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(names.Order(StringComparer.Ordinal), round_trip_replay.Replay(names, directory).Order(StringComparer.Ordinal));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
