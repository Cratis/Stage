// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cratis.Stage.Host;

/// <summary>
/// Whether a journey step posts a command or reads a query.
/// </summary>
internal enum StageJourneyStepKind
{
    /// <summary>
    /// The step posts a modeled command.
    /// </summary>
    Command,

    /// <summary>
    /// The step reads a modeled query.
    /// </summary>
    Query
}

/// <summary>
/// The outcomes a journey step reports.
/// </summary>
internal static class StageJourneyOutcome
{
    internal const string Accepted = "accepted";
    internal const string Rejected = "rejected";
    internal const string Unauthorized = "unauthorized";
    internal const string Unsupported = "unsupported";
    internal const string Error = "error";
    internal static readonly string[] Expectable = [Accepted, Rejected, Unauthorized, Unsupported];
}

/// <summary>
/// What a step expects, as written in the journey file; strings may still hold bindings.
/// </summary>
/// <param name="Outcome">The expected outcome.</param>
/// <param name="Messages">Texts each of which some actual message must contain.</param>
/// <param name="Response">The subset the command response must match.</param>
/// <param name="Result">The subset the query data must match.</param>
/// <param name="Absent">Values no element of the query data may match.</param>
/// <param name="Empty">Whether the query must return no rows.</param>
/// <param name="List">How arrays are matched.</param>
internal sealed record StageJourneyExpectation(
    string Outcome,
    IReadOnlyList<string> Messages,
    JsonElement? Response,
    JsonElement? Result,
    IReadOnlyList<JsonElement> Absent,
    bool Empty,
    StageJourneyListMatch List)
{
    internal static StageJourneyExpectation Success { get; } = new(StageJourneyOutcome.Accepted, [], null, null, [], false, StageJourneyListMatch.Contains);
}

/// <summary>
/// One step of a journey.
/// </summary>
/// <param name="Index">The zero-based index of the step in its journey.</param>
/// <param name="Name">The optional step name.</param>
/// <param name="Kind">Whether the step is a command or a query.</param>
/// <param name="Target">The modeled command or query name.</param>
/// <param name="Payload">The command input or the query arguments.</param>
/// <param name="Expect">What the step expects.</param>
/// <param name="ExpectAsWritten">The <c language="text">expect</c> object as written, echoed in the report.</param>
internal sealed record StageJourneyStep(int Index, string? Name, StageJourneyStepKind Kind, string Target, JsonElement Payload, StageJourneyExpectation Expect, JsonElement? ExpectAsWritten)
{
    /// <summary>
    /// Gets the values the step captures once it passes, by binding name and path.
    /// </summary>
    internal IReadOnlyDictionary<string, string> Bind { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets how long a query step re-reads until its expectation matches.
    /// </summary>
    internal TimeSpan Within { get; init; } = StageJourneyFile.DefaultWithin;

    /// <summary>
    /// Gets the free-text note, echoed in the report.
    /// </summary>
    internal string? Note { get; init; }

    /// <summary>
    /// Gets the evidence references, echoed in the report.
    /// </summary>
    internal IReadOnlyList<string>? Evidence { get; init; }
}

/// <summary>
/// One journey: steps replayed in order against the same session.
/// </summary>
/// <param name="Name">The unique journey name.</param>
/// <param name="Steps">The steps.</param>
internal sealed record StageJourney(string Name, IReadOnlyList<StageJourneyStep> Steps)
{
    /// <summary>
    /// Gets the free-text note, echoed in the report.
    /// </summary>
    internal string? Note { get; init; }

    /// <summary>
    /// Gets the evidence references, echoed in the report.
    /// </summary>
    internal IReadOnlyList<string>? Evidence { get; init; }
}

/// <summary>
/// The result of reading a journey file: the file, or every problem found in it.
/// </summary>
/// <param name="File">The file, or <see langword="null"/> when it has problems.</param>
/// <param name="Errors">Every problem, each qualified with its path.</param>
internal sealed record StageJourneyFileParse(StageJourneyFile? File, IReadOnlyList<string> Errors);

/// <summary>
/// A <c language="text">stage-journey/v1</c> file: recorded journeys to replay against a running sandbox.
/// </summary>
/// <param name="Journeys">The journeys.</param>
internal sealed partial record StageJourneyFile(IReadOnlyList<StageJourney> Journeys)
{
    internal const string Schema = "stage-journey/v1";
    internal const int MaxBodyBytes = 1024 * 1024;
    internal const int MaxJourneys = 100;
    internal const int MaxStepsPerJourney = 100;
    internal const int MaxSteps = 500;
    internal const int MaxPayloadDepth = 32;
    internal const int MaxNameLength = 200;
    internal const int MaxNoteLength = 1000;
    internal const int MaxWithinMilliseconds = 30000;
    internal static readonly TimeSpan DefaultWithin = TimeSpan.FromMilliseconds(5000);
    internal static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(30);

    static readonly string[] _fileProperties = ["schema", "journeys"];
    static readonly string[] _journeyProperties = ["name", "steps", "note", "evidence"];
    static readonly string[] _stepProperties = ["name", "command", "query", "input", "arguments", "expect", "bind", "within", "note", "evidence"];
    static readonly string[] _expectProperties = ["outcome", "messages", "response", "result", "absent", "empty", "list"];

    /// <summary>
    /// Gets the pattern a <c language="text">bind</c> path must match.
    /// </summary>
    [GeneratedRegex(@"^\$\.(response|result)(\.[A-Za-z_][A-Za-z0-9_]*|\[[0-9]+\])*$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    internal static partial Regex BindPath { get; }

    /// <summary>
    /// Reads a journey file, collecting every problem rather than stopping at the first.
    /// </summary>
    /// <param name="text">The file content.</param>
    /// <returns>The file, or the problems found.</returns>
    internal static StageJourneyFileParse Parse(string text)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 64 });
        }
        catch (JsonException exception)
        {
            return new(null, [$"$: invalid JSON: {exception.Message}"]);
        }

        using (document)
        {
            var reader = new Reader();
            var file = reader.File(document.RootElement);

            return reader.Errors.Count == 0 ? new(file, []) : new(null, reader.Errors);
        }
    }

    sealed class Reader
    {
        internal List<string> Errors { get; } = [];

        internal StageJourneyFile File(JsonElement root)
        {
            if (!Object(root, "$", _fileProperties)) return new([]);
            if (!root.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.String || schema.GetString() != Schema)
            {
                Errors.Add($"schema: must be '{Schema}'");
            }

            if (!root.TryGetProperty("journeys", out var journeys) || journeys.ValueKind != JsonValueKind.Array || journeys.GetArrayLength() == 0)
            {
                Errors.Add("journeys: must be a non-empty array");
                return new([]);
            }

            if (journeys.GetArrayLength() > MaxJourneys) Errors.Add($"journeys: at most {MaxJourneys} journeys");
            var names = new HashSet<string>(StringComparer.Ordinal);
            var read = journeys.EnumerateArray().Select((journey, index) => Journey(journey, $"journeys[{index}]", names)).ToArray();
            if (read.Sum(journey => journey.Steps.Count) > MaxSteps) Errors.Add($"journeys: at most {MaxSteps} steps in total");

            return new(read);
        }

        static int Depth(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.Object => 1 + element.EnumerateObject().Select(property => Depth(property.Value)).DefaultIfEmpty(0).Max(),
            JsonValueKind.Array => 1 + element.EnumerateArray().Select(Depth).DefaultIfEmpty(0).Max(),
            _ => 0
        };

        StageJourney Journey(JsonElement journey, string path, HashSet<string> names)
        {
            if (!Object(journey, path, _journeyProperties)) return new(string.Empty, []);
            var name = Name(journey, path, required: true) ?? string.Empty;
            if (name.Length > 0 && !names.Add(name)) Errors.Add($"{path}.name: '{name}' is used by an earlier journey");
            if (!journey.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array || steps.GetArrayLength() is 0 or > MaxStepsPerJourney)
            {
                Errors.Add($"{path}.steps: must be an array of 1 to {MaxStepsPerJourney} steps");
                return new(name, []);
            }

            return new(name, [.. steps.EnumerateArray().Select((step, index) => Step(step, $"{path}.steps[{index}]", index))])
            {
                Note = Note(journey, path),
                Evidence = Evidence(journey, path)
            };
        }

        StageJourneyStep Step(JsonElement step, string path, int index)
        {
            if (step.ValueKind == JsonValueKind.Object && step.TryGetProperty("persona", out _))
            {
                Errors.Add($"{path}.persona: not supported: the sandbox plays the persona each operation requires");
            }

            if (!Object(step, path, [.. _stepProperties, "persona"])) return new(index, null, StageJourneyStepKind.Command, string.Empty, default, StageJourneyExpectation.Success, null);
            var command = step.TryGetProperty("command", out var commandName);
            var query = step.TryGetProperty("query", out var queryName);
            if (command == query) Errors.Add($"{path}: exactly one of command or query");
            var kind = query && !command ? StageJourneyStepKind.Query : StageJourneyStepKind.Command;
            var target = kind == StageJourneyStepKind.Query ? queryName : commandName;
            if (command != query && (target.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(target.GetString())))
            {
                Errors.Add($"{path}.{(command ? "command" : "query")}: must be a non-empty model name");
            }

            var expect = step.TryGetProperty("expect", out var written) ? written.Clone() : (JsonElement?)null;
            return new(index, Name(step, path, required: false), kind, target.ValueKind == JsonValueKind.String ? target.GetString()! : string.Empty, Payload(step, path, kind), Expect(expect, $"{path}.expect", kind), expect)
            {
                Bind = Bind(step, path, kind),
                Within = Within(step, path, kind),
                Note = Note(step, path),
                Evidence = Evidence(step, path)
            };
        }

        JsonElement Payload(JsonElement step, string path, StageJourneyStepKind kind)
        {
            var (own, other) = kind == StageJourneyStepKind.Command ? ("input", "arguments") : ("arguments", "input");
            if (step.TryGetProperty(other, out _)) Errors.Add($"{path}.{other}: only for a {(kind == StageJourneyStepKind.Command ? "query" : "command")} step");
            if (!step.TryGetProperty(own, out var payload)) return JsonSerializer.SerializeToElement(new { });
            if (payload.ValueKind != JsonValueKind.Object)
            {
                Errors.Add($"{path}.{own}: must be an object");
            }
            else if (Depth(payload) > MaxPayloadDepth)
            {
                Errors.Add($"{path}.{own}: nested deeper than {MaxPayloadDepth} levels");
            }
            else if (kind == StageJourneyStepKind.Query)
            {
                foreach (var argument in payload.EnumerateObject().Where(argument => argument.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)))
                {
                    Errors.Add($"{path}.arguments.{argument.Name}: must be a string, number or boolean");
                }
            }

            return payload.Clone();
        }

        StageJourneyExpectation Expect(JsonElement? written, string path, StageJourneyStepKind kind)
        {
            if (written is not { } expect) return StageJourneyExpectation.Success;
            if (!Object(expect, path, _expectProperties)) return StageJourneyExpectation.Success;
            var outcome = Outcome(expect, path);
            var accepted = outcome == StageJourneyOutcome.Accepted;
            var messages = Strings(expect, "messages", path);
            if (messages.Count > 0 && accepted) Errors.Add($"{path}.messages: only with a rejected, unauthorized or unsupported outcome");
            var response = Only(expect, "response", path, kind == StageJourneyStepKind.Command, accepted);
            var result = Only(expect, "result", path, kind == StageJourneyStepKind.Query, accepted);
            var absent = Only(expect, "absent", path, kind == StageJourneyStepKind.Query, accepted);
            if (absent is { ValueKind: not JsonValueKind.Array }) Errors.Add($"{path}.absent: must be an array");
            var empty = Only(expect, "empty", path, kind == StageJourneyStepKind.Query, accepted);
            if (empty is { ValueKind: not (JsonValueKind.True or JsonValueKind.False) }) Errors.Add($"{path}.empty: must be a boolean");
            if (empty is { ValueKind: JsonValueKind.True } && result is not null) Errors.Add($"{path}.empty: cannot be combined with result");

            return new(
                outcome,
                messages,
                response,
                result,
                absent is { ValueKind: JsonValueKind.Array } items ? [.. items.EnumerateArray()] : [],
                empty is { ValueKind: JsonValueKind.True },
                List(expect, path));
        }

        string Outcome(JsonElement expect, string path)
        {
            if (!expect.TryGetProperty("outcome", out var outcome)) return StageJourneyOutcome.Accepted;
            if (outcome.ValueKind == JsonValueKind.String && StageJourneyOutcome.Expectable.Contains(outcome.GetString())) return outcome.GetString()!;
            Errors.Add($"{path}.outcome: must be one of {string.Join(", ", StageJourneyOutcome.Expectable)}");

            return StageJourneyOutcome.Accepted;
        }

        JsonElement? Only(JsonElement expect, string property, string path, bool kindAllows, bool accepted)
        {
            if (!expect.TryGetProperty(property, out var value)) return null;
            if (!kindAllows)
            {
                Errors.Add($"{path}.{property}: not for this kind of step");
            }
            else if (!accepted)
            {
                Errors.Add($"{path}.{property}: only with an accepted outcome");
            }

            return value.Clone();
        }

        StageJourneyListMatch List(JsonElement expect, string path)
        {
            if (!expect.TryGetProperty("list", out var list)) return StageJourneyListMatch.Contains;
            switch (list.ValueKind == JsonValueKind.String ? list.GetString() : null)
            {
                case "contains": return StageJourneyListMatch.Contains;
                case "ordered": return StageJourneyListMatch.Ordered;
                case "exact": return StageJourneyListMatch.Exact;
                default:
                    Errors.Add($"{path}.list: must be one of contains, ordered, exact");
                    return StageJourneyListMatch.Contains;
            }
        }

        Dictionary<string, string> Bind(JsonElement step, string path, StageJourneyStepKind kind)
        {
            var bind = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!step.TryGetProperty("bind", out var written)) return bind;
            if (written.ValueKind != JsonValueKind.Object)
            {
                Errors.Add($"{path}.bind: must be an object of binding names to paths");
                return bind;
            }

            var root = kind == StageJourneyStepKind.Command ? "$.response" : "$.result";
            foreach (var binding in written.EnumerateObject())
            {
                var target = binding.Value.ValueKind == JsonValueKind.String ? binding.Value.GetString()! : string.Empty;
                if (!BindPath.IsMatch(target) || !target.StartsWith(root, StringComparison.Ordinal))
                {
                    Errors.Add($"{path}.bind.{binding.Name}: must be a path under {root} of .name and [index] segments");
                    continue;
                }

                bind[binding.Name] = target;
            }

            return bind;
        }

        TimeSpan Within(JsonElement step, string path, StageJourneyStepKind kind)
        {
            if (!step.TryGetProperty("within", out var within)) return DefaultWithin;
            if (kind != StageJourneyStepKind.Query) Errors.Add($"{path}.within: only for a query step");
            if (!within.TryGetInt32(out var milliseconds))
            {
                Errors.Add($"{path}.within: must be a whole number of milliseconds");
                return DefaultWithin;
            }

            return TimeSpan.FromMilliseconds(Math.Clamp(milliseconds, 0, MaxWithinMilliseconds));
        }

        string? Name(JsonElement owner, string path, bool required)
        {
            if (!owner.TryGetProperty("name", out var name))
            {
                if (required) Errors.Add($"{path}.name: is required");
                return null;
            }

            if (name.ValueKind != JsonValueKind.String || name.GetString()!.Length is 0 or > MaxNameLength)
            {
                Errors.Add($"{path}.name: must be a string of 1 to {MaxNameLength} characters");
                return null;
            }

            return name.GetString();
        }

        string? Note(JsonElement owner, string path)
        {
            if (!owner.TryGetProperty("note", out var note)) return null;
            if (note.ValueKind == JsonValueKind.String && note.GetString()!.Length <= MaxNoteLength) return note.GetString();
            Errors.Add($"{path}.note: must be a string of at most {MaxNoteLength} characters");

            return null;
        }

        List<string>? Evidence(JsonElement owner, string path) =>
            owner.TryGetProperty("evidence", out _) ? Strings(owner, "evidence", path) : null;

        List<string> Strings(JsonElement owner, string property, string path)
        {
            if (!owner.TryGetProperty(property, out var values)) return [];
            if (values.ValueKind != JsonValueKind.Array || values.EnumerateArray().Any(value => value.ValueKind != JsonValueKind.String))
            {
                Errors.Add($"{path}.{property}: must be an array of strings");
                return [];
            }

            return [.. values.EnumerateArray().Select(value => value.GetString()!)];
        }

        bool Object(JsonElement element, string path, string[] allowed)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                Errors.Add($"{path}: must be an object");
                return false;
            }

            foreach (var property in element.EnumerateObject().Where(property => !allowed.Contains(property.Name, StringComparer.Ordinal)))
            {
                Errors.Add($"{(path == "$" ? string.Empty : $"{path}.")}{property.Name}: unknown property");
            }

            return true;
        }
    }
}
