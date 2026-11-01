// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Stage.Publishing;

namespace Cratis.Stage.Host.Export;

/// <summary>
/// The <c language="shell">export</c> command of the Stage host: <c language="shell">Cratis.Stage.Host export --model &lt;path&gt; --version &lt;semver&gt; --name &lt;publishedName&gt; --output &lt;dir&gt;</c>.
/// </summary>
internal static class StageExportCommand
{
    internal const string Usage =
        "Usage: export --model <path> --version <semver> --name <publishedName> --output <dir> " +
        "[--application-name <name>] [--publication-id <id>] [--notes] [--runtime-version <semver>] [--references <file.json>]";

    /// <summary>
    /// Runs the command.
    /// </summary>
    /// <param name="arguments">The arguments after <c language="shell">export</c>.</param>
    /// <param name="output">Where messages go.</param>
    /// <param name="error">Where refusals go.</param>
    /// <returns>The process exit code: 0 on success, 1 when the model is refused, 2 on a usage error.</returns>
    internal static async Task<int> Run(string[] arguments, TextWriter output, TextWriter error)
    {
        try
        {
            var request = Parse(arguments);
            var folder = await StageExport.Run(request);
            await output.WriteLineAsync($"Exported {request.PublishedName} {request.ApplicationVersion} to {folder}");

            return 0;
        }
        catch (StageExportRefused refused)
        {
            await error.WriteLineAsync(refused.Message);

            return 1;
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException or IOException)
        {
            await error.WriteLineAsync($"{exception.Message}{Environment.NewLine}{Usage}");

            return 2;
        }
    }

    internal static StageExportRequest Parse(string[] arguments)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var notes = false;
        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            if (argument == "--notes")
            {
                notes = true;
            }
            else if (argument.StartsWith("--", StringComparison.Ordinal) && index + 1 < arguments.Length)
            {
                values[argument] = arguments[++index];
            }
            else
            {
                throw new ArgumentException($"Unexpected argument '{argument}'.");
            }
        }

        string Required(string name) => values.TryGetValue(name, out var value) ? value : throw new ArgumentException($"Missing {name}.");

        var references = ReadReferences(values.GetValueOrDefault("--references"));

        return new StageExportRequest(Required("--model"), Required("--version"), Required("--name"), Required("--output"))
        {
            ApplicationName = values.GetValueOrDefault("--application-name"),
            PublicationId = values.GetValueOrDefault("--publication-id"),
            NotesEnabled = notes,
            RuntimeVersion = values.GetValueOrDefault("--runtime-version"),
            ModelReferences = references
        };
    }

    static Dictionary<string, string> ReadReferences(string? file)
    {
        if (file is null)
        {
            return [];
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)) ?? [];
    }
}
