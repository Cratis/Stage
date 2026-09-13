// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Naming;

namespace Cratis.Stage.Host;

/// <summary>
/// Resolves the Chronicle event store name a container's Stage processes share.
/// </summary>
/// <remarks>
/// The event store belongs to the container, not to a process. A warm Stage boots, registers a store in the
/// bundled kernel, and restarts itself against the model it is later handed - and a name generated per process
/// made that restart register a <em>second</em> store beside the first. A play session's Chronicle Workbench then
/// listed two stores where the session only ever has one, and the abandoned one kept registrations nothing was
/// running. Resolving the name once and remembering it for the container's lifetime is what keeps the session to
/// a single store across the handoff restart.
/// </remarks>
public static class ContainerEventStoreName
{
    /// <summary>
    /// The environment variable that pins the name explicitly.
    /// </summary>
    public const string Variable = "STAGE_EVENT_STORE";

    /// <summary>
    /// The environment variable naming the directory the resolved name is remembered in.
    /// </summary>
    public const string StateDirectoryVariable = "STAGE_STATE_DIR";

    /// <summary>
    /// The file the resolved name is remembered in, within the state directory.
    /// </summary>
    public const string FileName = "event-store";

    /// <summary>
    /// Resolves the event store name for this process.
    /// </summary>
    /// <returns>The pinned name, the one a previous process in this container resolved, or a newly generated one.</returns>
    public static string Resolve() => Resolve(
        Environment.GetEnvironmentVariable(Variable),
        Environment.GetEnvironmentVariable(StateDirectoryVariable) is { Length: > 0 } directory ? directory : Path.GetTempPath(),
        DockerStyleName.Generate);

    /// <summary>
    /// Resolves the event store name from explicit inputs.
    /// </summary>
    /// <param name="pinned">The explicitly configured name, if any.</param>
    /// <param name="stateDirectory">The directory the resolved name is remembered in.</param>
    /// <param name="generate">Generates a name when there is nothing to reuse.</param>
    /// <returns>The event store name every process in this container uses.</returns>
    public static string Resolve(string? pinned, string stateDirectory, Func<string> generate)
    {
        if (!string.IsNullOrWhiteSpace(pinned))
        {
            return pinned;
        }

        var path = Path.Combine(stateDirectory, FileName);

        try
        {
            if (File.Exists(path) && File.ReadAllText(path).Trim() is { Length: > 0 } remembered)
            {
                return remembered;
            }
        }
        catch (IOException)
        {
            // An unreadable note is no worse than no note: a fresh name still runs the session, it only costs
            // the Workbench an extra store. Failing the whole Stage over it would be the wrong trade.
        }

        var name = generate();

        try
        {
            Directory.CreateDirectory(stateDirectory);
            File.WriteAllText(path, name);
        }
        catch (IOException)
        {
            // Same reasoning as above, in the other direction.
        }

        return name;
    }
}
