// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;

/// <summary>
/// A container lifecycle wired to scripted ports.
/// </summary>
public class a_container : Specification
{
    protected static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    protected static readonly GeneratedApplicationContainerId Own = Id('a');
    protected static readonly GeneratedApplicationContainerId Other = Id('b');
    protected static readonly GeneratedApplicationContainerId Third = Id('c');

    protected ScriptedDockerCommands _docker = null!;
    protected FakeOwnerProcesses _processes = null!;
    protected GeneratedApplicationContainerOwner _owner = null!;
    protected GeneratedApplicationContainer _container = null!;
    protected string _cidFile = null!;
    string _directory = null!;

    void Establish()
    {
        _docker = new();
        _processes = new();
        _owner = new() { Host = "host-a", ProcessId = 4242, ProcessStartedUnixSeconds = 1_000_000, RunToken = "0123abcd0123abcd", StartedUnixSeconds = Now.ToUnixTimeSeconds() };
        _directory = Directory.CreateTempSubdirectory("container-lifecycle-spec-").FullName;
        _cidFile = Path.Combine(_directory, "container.cid");
        _container = new(_docker, _processes, new FakeClock(Now), _owner, new("stage-generated-name", "image:1", _cidFile));
    }

    void Destroy() => Directory.Delete(_directory, true);

    protected static GeneratedApplicationContainerId Id(char character) => new(new string(character, 64));

    protected static DockerCommandResult Success(string output = "") => new(0, output, string.Empty);

    protected static DockerCommandResult Failure(string error) => new(1, string.Empty, error);

    protected static DockerCommandResult Missing() => Failure("Error: No such object: gone");

    protected static DockerCommandResult Inspected(GeneratedApplicationContainerId id, GeneratedApplicationContainerOwner owner)
    {
        var labels = owner.Labels().Chunk(2).Select(pair => pair[1].Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);
        return Success($"{id}\n{JsonSerializer.Serialize(labels)}\n");
    }

    protected static GeneratedApplicationContainerOwner OwnedBy(string runToken, long startedUnixSeconds) => new() { Host = "host-a", ProcessId = 1, ProcessStartedUnixSeconds = 1, RunToken = runToken, StartedUnixSeconds = startedUnixSeconds };

    /// <summary>
    /// Scripts docker create, writing the identifier file the way docker does.
    /// </summary>
    /// <param name="cidContent">What docker writes to the identifier file, or null when it writes nothing.</param>
    /// <param name="result">The command result.</param>
    protected void ScriptCreate(string? cidContent, DockerCommandResult result)
    {
        void WriteIdentifier(IReadOnlyList<string> arguments)
        {
            if (cidContent is not null)
            {
                File.WriteAllText(arguments[2], cidContent);
            }
        }

        _docker.On(arguments => arguments[0] == "create", WriteIdentifier, result);
    }

    protected void ScriptOwnedInspect(GeneratedApplicationContainerId id) => _docker.On("inspect", Inspected(id, _owner));

    protected IEnumerable<string> Commands(string verb) => _docker.Calls.Where(call => call[0] == verb).Select(call => string.Join(' ', call));
}
