// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// What docker inspect established about one container.
/// </summary>
public sealed record GeneratedApplicationContainerInspection
{
    /// <summary>Gets Found, Missing or Unknown.</summary>
    public required GeneratedApplicationContainerInspectionKind Kind { get; init; }

    /// <summary>Gets the identifier read from the container when found.</summary>
    public GeneratedApplicationContainerId? Id { get; init; }

    /// <summary>Gets the labels read from the container when found.</summary>
    public required IReadOnlyDictionary<string, string> Labels { get; init; }

    /// <summary>Gets why the result is Missing or Unknown.</summary>
    public required string Reason { get; init; }

    /// <summary>
    /// The projection that keeps the container environment out of the output: only the identifier and the labels.
    /// </summary>
    public const string Format = "{{.Id}}{{\"\\n\"}}{{json .Config.Labels}}";

    /// <summary>
    /// Builds the arguments that inspect one container by its full identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The arguments after docker.</returns>
    public static IReadOnlyList<string> Arguments(GeneratedApplicationContainerId id) => ["inspect", "--type", "container", "--format", Format, id.Value];

    /// <summary>
    /// Parses projected inspect output. Anything that is not a clean answer is Unknown, never success.
    /// </summary>
    /// <param name="result">The command result.</param>
    /// <param name="expectedId">The identifier that was inspected, when it must be confirmed.</param>
    /// <returns>The inspection.</returns>
    public static GeneratedApplicationContainerInspection Parse(DockerCommandResult result, GeneratedApplicationContainerId? expectedId = null)
    {
        if (result.ExitCode != 0)
        {
            return string.IsNullOrWhiteSpace(result.StandardOutput) && result.StandardError.Contains("No such object", StringComparison.OrdinalIgnoreCase)
                ? Without(GeneratedApplicationContainerInspectionKind.Missing, "no such object")
                : Unknown($"inspect failed with exit code {result.ExitCode}");
        }

        var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length != 2 || !GeneratedApplicationContainerId.TryParse(lines[0], out var id))
        {
            return Unknown("inspect output was not an identifier followed by labels");
        }

        if (expectedId is not null && id != expectedId)
        {
            return Unknown("inspect returned a different container identifier");
        }

        return TryReadLabels(lines[1], out var labels)
            ? new() { Kind = GeneratedApplicationContainerInspectionKind.Found, Id = id, Labels = labels, Reason = string.Empty }
            : Unknown("inspect labels were not a JSON object of strings");
    }

    static GeneratedApplicationContainerInspection Unknown(string reason) => Without(GeneratedApplicationContainerInspectionKind.Unknown, reason);

    static GeneratedApplicationContainerInspection Without(GeneratedApplicationContainerInspectionKind kind, string reason) => new() { Kind = kind, Labels = Empty(), Reason = reason };

    static Dictionary<string, string> Empty() => [];

    static bool TryReadLabels(string json, out IReadOnlyDictionary<string, string> labels)
    {
        labels = Empty();
        try
        {
            labels = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? Empty();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
