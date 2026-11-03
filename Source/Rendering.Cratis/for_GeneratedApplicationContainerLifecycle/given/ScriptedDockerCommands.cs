// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.given;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;

/// <summary>
/// A scripted container command port. An unexpected command throws, so nothing silently succeeds.
/// </summary>
public class ScriptedDockerCommands : IDockerCommands
{
    readonly List<(Func<IReadOnlyList<string>, bool> Match, Queue<DockerCommandResult> Results, DockerCommandResult Last, Action<IReadOnlyList<string>>? Effect)> _rules = [];

    /// <summary>
    /// Gets every command that ran, in order.
    /// </summary>
    public List<string[]> Calls { get; } = [];

    /// <summary>
    /// Scripts the results for commands whose first argument matches. The last result repeats.
    /// </summary>
    /// <param name="match">Chooses the commands.</param>
    /// <param name="effect">A side effect to run with the command, or null.</param>
    /// <param name="results">The results in order.</param>
    public void On(Func<IReadOnlyList<string>, bool> match, Action<IReadOnlyList<string>>? effect, params DockerCommandResult[] results) =>
        _rules.Add((match, new(results), results[^1], effect));

    /// <summary>
    /// Scripts a command by its leading arguments.
    /// </summary>
    /// <param name="prefix">The leading arguments.</param>
    /// <param name="results">The results in order.</param>
    public void On(string prefix, params DockerCommandResult[] results) => On(arguments => string.Join(' ', arguments).StartsWith(prefix, StringComparison.Ordinal), null, results);

    /// <summary>
    /// Gets how many commands started with a verb.
    /// </summary>
    /// <param name="verb">The first argument.</param>
    /// <returns>The count.</returns>
    public int Count(string verb) => Calls.Count(call => call[0] == verb);

    /// <inheritdoc/>
    public Task<DockerCommandResult> Run(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        Calls.Add([.. arguments]);
        foreach (var rule in _rules.Where(rule => rule.Match(arguments)))
        {
            rule.Effect?.Invoke(arguments);
            return Task.FromResult(rule.Results.Count > 0 ? rule.Results.Dequeue() : rule.Last);
        }

        throw new InvalidOperationException($"Unexpected docker command: {arguments[0]}");
    }
}
