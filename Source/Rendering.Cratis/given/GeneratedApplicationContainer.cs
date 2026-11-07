// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// One container owned for its whole life: labelled at create, identified by its full identifier, and removed only
/// through the ownership guard.
/// </summary>
/// <remarks>
/// The name is for diagnostics. No command in this type targets a container by name.
/// </remarks>
public sealed class GeneratedApplicationContainer
{
    /// <summary>
    /// How long creating the container, including pulling its image, may take.
    /// </summary>
    public static readonly TimeSpan CreateTimeout = TimeSpan.FromMinutes(8);

    /// <summary>
    /// How long the removal at process exit may take in total.
    /// </summary>
    public static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(30);

    readonly IDockerCommands _docker;
    readonly GeneratedApplicationContainerGuard _guard;
    readonly GeneratedApplicationContainerReaper _reaper;
    readonly GeneratedApplicationContainerOwner _owner;
    readonly GeneratedApplicationContainerDefinition _definition;
    readonly SemaphoreSlim _gate = new(1, 1);
    GeneratedApplicationContainerId? _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedApplicationContainer"/> class.
    /// </summary>
    /// <param name="docker">Runs the container commands.</param>
    /// <param name="processes">Probes owning processes while reaping.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="owner">The owner of this run.</param>
    /// <param name="definition">What to create.</param>
    public GeneratedApplicationContainer(IDockerCommands docker, IOwnerProcesses processes, TimeProvider clock, GeneratedApplicationContainerOwner owner, GeneratedApplicationContainerDefinition definition)
    {
        _docker = docker;
        _guard = new(docker);
        _reaper = new(docker, _guard, processes, clock, owner);
        _owner = owner;
        _definition = definition;
    }

    /// <summary>
    /// Removes stale containers from earlier killed runs.
    /// </summary>
    /// <returns>The report.</returns>
    public Task<ContainerReapReport> ReapStale() => _reaper.Reap();

    /// <summary>
    /// Creates the labelled container and confirms its identifier.
    /// </summary>
    /// <returns>The outcome. A name collision fails here and touches nothing.</returns>
    public async Task<ContainerOutcome> Create()
    {
        if (File.Exists(_definition.CidFilePath))
        {
            File.Delete(_definition.CidFilePath);
        }

        var result = await _docker.Run(CreateArguments(), CreateTimeout, CancellationToken.None);
        var text = File.Exists(_definition.CidFilePath) ? (await File.ReadAllTextAsync(_definition.CidFilePath)).Trim() : string.Empty;
        if (!GeneratedApplicationContainerId.TryParse(text, out var id))
        {
            return new(false, result.ExitCode == 0 ? "docker create did not report a valid container identifier" : result.Output);
        }

        var unconfirmed = await _guard.Unconfirmed(id, _owner.RunToken);
        if (unconfirmed.Length > 0)
        {
            return new(false, $"the created container could not be confirmed as owned by this run: {unconfirmed}");
        }

        await Locked(() => _id = id);
        return result.ExitCode == 0 ? new(true, string.Empty) : new(false, result.Output);
    }

    /// <summary>
    /// Starts the created container.
    /// </summary>
    /// <returns>The outcome.</returns>
    public async Task<ContainerOutcome> Start()
    {
        if (_id is null)
        {
            return new(false, "the container was not created");
        }

        var result = await _docker.Run(["start", _id.Value], GeneratedApplicationContainerGuard.CommandTimeout, CancellationToken.None);
        return new(result.ExitCode == 0, result.Output);
    }

    /// <summary>
    /// Queries a mapped host port of the container.
    /// </summary>
    /// <param name="containerPort">The container port and protocol, such as 27017/tcp.</param>
    /// <returns>The command result.</returns>
    public Task<DockerCommandResult> Port(string containerPort) => Command("port", containerPort);

    /// <summary>
    /// Reads the container logs.
    /// </summary>
    /// <returns>The command result.</returns>
    public Task<DockerCommandResult> Logs() => Command("logs");

    /// <summary>
    /// Removes the container if this run created it. Safe to call repeatedly and concurrently.
    /// </summary>
    /// <returns>The outcome. Without a confirmed identifier nothing is removed.</returns>
    public async Task<ContainerRemovalResult> Remove()
    {
        await _gate.WaitAsync();
        try
        {
            if (_id is null)
            {
                return new(ContainerRemoval.NotCreated, string.Empty);
            }

            var result = await _guard.Remove(_id, _owner.RunToken);
            if (result.Outcome is ContainerRemoval.Removed or ContainerRemoval.AlreadyGone)
            {
                _id = null;
            }

            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Removes the container when the process exits. It never throws and never waits longer than <see cref="ExitTimeout"/>.
    /// </summary>
    public void RemoveWhenExiting()
    {
        try
        {
            Task.Run(Remove).Wait(ExitTimeout);
        }
        catch (Exception exception) when (exception is AggregateException or ObjectDisposedException)
        {
            Console.Error.WriteLine($"Removing the generated-application container at exit failed: {exception.GetType().Name}");
        }
    }

    List<string> CreateArguments()
    {
        List<string> arguments = ["create", "--cidfile", _definition.CidFilePath];
        arguments.AddRange(_owner.Labels());
        arguments.AddRange(["--name", _definition.Name, "--publish", "127.0.0.1::27017", "--publish", "127.0.0.1::35000", _definition.Image]);
        return arguments;
    }

    async Task<DockerCommandResult> Command(params string[] arguments)
    {
        var id = _id;
        return id is null
            ? new(-1, string.Empty, "the container was not created")
            : await _docker.Run([arguments[0], id.Value, .. arguments[1..]], GeneratedApplicationContainerGuard.CommandTimeout, CancellationToken.None);
    }

    async Task Locked(Action action)
    {
        await _gate.WaitAsync();
        try
        {
            action();
        }
        finally
        {
            _gate.Release();
        }
    }
}
