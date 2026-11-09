// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using static Cratis.Stage.Host.StageJourneyStepReports;

namespace Cratis.Stage.Host;

/// <summary>
/// Maps the sandbox route that replays recorded journeys against the running model.
/// </summary>
internal static class StageJourneyReplay
{
    internal const string Route = "/stage/semantic/journeys/replay";
    static readonly string[] _wildcardHosts = ["+", "*", "0.0.0.0", "[::]", "::"];

    internal static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    /// <summary>
    /// Maps <see cref="Route"/>; one replay runs at a time.
    /// </summary>
    /// <param name="app">The application to map on.</param>
    /// <param name="routes">Builds the modeled routes, or answers <see langword="null"/> when the model has none.</param>
    /// <param name="status">Reads the session status.</param>
    internal static void Map(IEndpointRouteBuilder app, Func<StageRoutes?> routes, Func<StageStatus> status)
    {
        var gate = new SemaphoreSlim(1, 1);
        app.MapPost(Route, (HttpContext context, StageJourneyClient client, StageJourneyOptions options) => Replay(context, new(routes, status, gate, client, options)));
    }

    /// <summary>
    /// Maps <see cref="Route"/> for a session whose model was refused: every replay is answered 503 with the issues, as the session reports them.
    /// </summary>
    /// <param name="app">The application to map on.</param>
    /// <param name="issues">The issues the model was refused with.</param>
    internal static void MapRefused(IEndpointRouteBuilder app, List<StageUnsupportedIssue> issues) =>
        app.MapPost(Route, () => Problem("The session is unsupported.", StatusCodes.Status503ServiceUnavailable, [.. issues.Select(issue => $"{issue.Capability} {issue.Artifact}: {issue.Details}")]));

    static async Task<IResult> Replay(HttpContext context, ReplayHost host)
    {
        var aborted = context.RequestAborted;
        if (!context.Request.HasJsonContentType()) return Problem("The journey file must be sent as application/json.", StatusCodes.Status415UnsupportedMediaType);
        if (context.Request.ContentLength > StageJourneyFile.MaxBodyBytes) return TooLarge();
        var text = await ReadBounded(context.Request.Body, aborted);
        if (text is null) return TooLarge();
        var parse = StageJourneyFile.Parse(text);
        if (parse.File is null) return Problem("The journey file is invalid.", StatusCodes.Status400BadRequest, parse.Errors);
        var requested = context.Request.Query["journey"].OfType<string>().ToArray();
        var unknown = requested.Where(name => parse.File.Journeys.All(journey => journey.Name != name)).ToArray();
        if (unknown.Length > 0) return Problem("The journey file is invalid.", StatusCodes.Status400BadRequest, [.. unknown.Select(name => $"journey: '{name}' is not in the file")]);
        var current = host.Status();
        if (current.State != "ready") return Problem($"The session is {current.State}.", StatusCodes.Status503ServiceUnavailable, [.. current.Issues?.Select(issue => $"{issue.Capability} {issue.Artifact}: {issue.Details}") ?? []]);
        var table = host.Routes();
        if (table is null) return Problem("The session has no scene routes to replay against.", StatusCodes.Status503ServiceUnavailable);
        var address = Loopback(context.RequestServices.GetService<IServer>());
        if (address is null) return Problem("The session has no HTTP address to replay against.", StatusCodes.Status503ServiceUnavailable);
        if (!await host.Gate.WaitAsync(0, aborted)) return Problem("A replay is already running.", StatusCodes.Status409Conflict);
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(aborted);
            deadline.CancelAfter(host.Options.Timeout);
            var journeys = requested.Length == 0 ? parse.File.Journeys : [.. parse.File.Journeys.Where(journey => requested.Contains(journey.Name))];
            var report = await new StageJourneyRunner(table, host.Client.Http, address, StageJourneyFile.StepTimeout).Run(journeys, deadline.Token);

            return Results.Json(report, JsonOptions);
        }
        catch (OperationCanceledException) when (!aborted.IsCancellationRequested)
        {
            return Problem("The replay timed out.", StatusCodes.Status504GatewayTimeout);
        }
        finally
        {
            host.Gate.Release();
        }
    }

    static async Task<string?> ReadBounded(Stream body, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > StageJourneyFile.MaxBodyBytes) return null;
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    static Uri? Loopback(IServer? server)
    {
        var addresses = server?.Features.Get<IServerAddressesFeature>()?.Addresses.Select(BindingAddress.Parse).Where(address => !address.IsUnixPipe && address.Port > 0).ToArray() ?? [];
        var chosen = addresses.FirstOrDefault(address => address.Scheme == Uri.UriSchemeHttp) ?? addresses.FirstOrDefault();
        if (chosen is null) return null;
        var host = _wildcardHosts.Contains(chosen.Host, StringComparer.Ordinal) ? "127.0.0.1" : chosen.Host;

        return new UriBuilder(chosen.Scheme, host, chosen.Port).Uri;
    }

    static IResult TooLarge() => Problem($"The journey file exceeds {StageJourneyFile.MaxBodyBytes} bytes.", StatusCodes.Status413PayloadTooLarge);

    static IResult Problem(string details, int status, IReadOnlyList<string>? errors = null)
    {
        var extensions = new Dictionary<string, object?> { ["capability"] = "Journey" };
        if (errors is { Count: > 0 }) extensions["errors"] = errors;

        return Results.Problem(detail: details, statusCode: status, title: "Journey replay failed", extensions: extensions);
    }

    sealed record ReplayHost(Func<StageRoutes?> Routes, Func<StageStatus> Status, SemaphoreSlim Gate, StageJourneyClient Client, StageJourneyOptions Options);
}

/// <summary>
/// How long a whole replay may take, read from <c language="text">Stage:Journeys:TimeoutSeconds</c>.
/// </summary>
/// <param name="Timeout">The whole-run timeout.</param>
internal sealed record StageJourneyOptions(TimeSpan Timeout)
{
    internal static StageJourneyOptions FromConfiguration(IConfiguration configuration) =>
        new(TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Stage:Journeys:TimeoutSeconds", 120), 1, 600)));
}

/// <summary>
/// The HTTP client a replay sends its loopback requests with; it carries no cookies and follows no redirects.
/// </summary>
/// <param name="handler">The message handler.</param>
internal sealed class StageJourneyClient(HttpMessageHandler handler) : IDisposable
{
    internal StageJourneyClient()
        : this(new SocketsHttpHandler { UseCookies = false, AllowAutoRedirect = false })
    {
    }

    internal HttpClient Http { get; } = new(handler) { Timeout = Timeout.InfiniteTimeSpan };

    /// <inheritdoc/>
    public void Dispose() => Http.Dispose();
}

/// <summary>
/// Replays journeys step by step over HTTP and compares each answer with what the step expects.
/// </summary>
/// <param name="routes">The modeled routes by name.</param>
/// <param name="client">The client the requests are sent with.</param>
/// <param name="address">The address of the running application.</param>
/// <param name="stepTimeout">How long one step may take.</param>
/// <param name="time">The clock date tokens read; defaults to the system clock.</param>
internal sealed class StageJourneyRunner(StageRoutes routes, HttpClient client, Uri address, TimeSpan stepTimeout, TimeProvider? time = null)
{
    internal const int ReportedBodyLimit = 64 * 1024;
    static readonly TimeSpan _reread = TimeSpan.FromMilliseconds(100);
    readonly TimeProvider _time = time ?? TimeProvider.System;

    /// <summary>
    /// Replays the journeys in order.
    /// </summary>
    /// <param name="journeys">The journeys to replay.</param>
    /// <param name="cancellationToken">Stops the run.</param>
    /// <returns>The report.</returns>
    internal async Task<StageJourneyReport> Run(IEnumerable<StageJourney> journeys, CancellationToken cancellationToken)
    {
        var results = new List<StageJourneyResult>();
        foreach (var journey in journeys)
        {
            results.Add(await Journey(journey, cancellationToken));
        }

        return StageJourneyReport.From(results);
    }

    async Task<StageJourneyResult> Journey(StageJourney journey, CancellationToken cancellationToken)
    {
        var bindings = new StageJourneyBindings(_time);
        var steps = new List<StageJourneyStepResult>();
        foreach (var step in journey.Steps)
        {
            steps.Add(steps.Exists(earlier => earlier.Status != StageJourneyStepStatus.Passed)
                ? StageJourneyStepReports.Report(step, StageJourneyStepStatus.Skipped, null)
                : await Step(step, bindings, cancellationToken));
        }

        return new(journey.Name, steps.TrueForAll(step => step.Status == StageJourneyStepStatus.Passed), steps) { Note = journey.Note, Evidence = journey.Evidence };
    }

    async Task<StageJourneyStepResult> Step(StageJourneyStep step, StageJourneyBindings bindings, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        if (!TryPrepare(step, bindings, out var request, out var expect, out var problem)) return Errored(step, watch, problem, bindings);
        try
        {
            var (answer, mismatches, attempts) = await Attempts(step, request, expect, cancellationToken);
            if (answer.Error is not null) return Errored(step, watch, answer.Error, bindings) with { HttpStatus = answer.HttpStatus, Actual = Actual(answer), Attempts = attempts };
            if (mismatches.Count == 0 && !bindings.TryCapture(step.Bind, answer.Body, out problem)) return Errored(step, watch, problem, bindings);
            var status = mismatches.Count == 0 ? StageJourneyStepStatus.Passed : StageJourneyStepStatus.Failed;

            return Report(step, status, watch, bindings) with
            {
                Route = request.Route,
                HttpStatus = answer.HttpStatus,
                Attempts = step.Kind == StageJourneyStepKind.Query ? attempts : null,
                Actual = status == StageJourneyStepStatus.Failed ? Actual(answer) : null,
                Mismatches = status == StageJourneyStepStatus.Failed ? mismatches : null
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Errored(step, watch, $"the step timed out after {stepTimeout.TotalSeconds:0} seconds", bindings);
        }
        catch (HttpRequestException exception)
        {
            return Errored(step, watch, $"the request failed: {exception.Message}", bindings);
        }
    }

    async Task<(StageJourneyAnswer Answer, IReadOnlyList<StageJourneyMismatch> Mismatches, int Attempts)> Attempts(StageJourneyStep step, PreparedRequest request, StageJourneyExpectation expect, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(stepTimeout);
        var watch = Stopwatch.StartNew();
        var attempts = 0;
        while (true)
        {
            attempts++;
            var answer = await Send(step.Kind, request, timeout.Token);
            var mismatches = answer.Error is null ? StageJourneyComparison.Compare(expect, answer) : [];
            if (answer.Error is not null || mismatches.Count == 0 || step.Kind == StageJourneyStepKind.Command || watch.Elapsed + _reread > step.Within)
            {
                return (answer, mismatches, attempts);
            }

            await Task.Delay(_reread, _time, timeout.Token);
        }
    }

    async Task<StageJourneyAnswer> Send(StageJourneyStepKind kind, PreparedRequest prepared, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(kind == StageJourneyStepKind.Command ? HttpMethod.Post : HttpMethod.Get, new Uri(address, prepared.Route));
        if (prepared.Body is not null) request.Content = new StringContent(prepared.Body, Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        return StageJourneyAnswer.Classify(kind, response, text);
    }

    bool TryPrepare(StageJourneyStep step, StageJourneyBindings bindings, out PreparedRequest request, out StageJourneyExpectation expect, out string problem)
    {
        request = new(string.Empty, null);
        expect = step.Expect;
        if (!TryResolve(step, out var route, out problem) ||
            !bindings.TrySubstitute(step.Payload, out var payload, out problem) ||
            !bindings.TrySubstitute(step.Expect, out expect, out problem))
        {
            return false;
        }

        request = step.Kind == StageJourneyStepKind.Command ? new(route, payload.GetRawText()) : new(StageJourneyStepReports.WithArguments(route, payload), null);

        return true;
    }

    bool TryResolve(StageJourneyStep step, out string route, out string problem)
    {
        var (table, kind, method) = step.Kind == StageJourneyStepKind.Command ? (routes.Commands, "command", "POST") : (routes.Queries, "query", "GET");
        var simple = StageRouteCandidates.SimpleName(step.Target);
        problem = string.Empty;

        // Queries are published by their simple name only, so a qualified query name resolves through it.
        if (table.TryGetValue(step.Target, out route!) || (step.Kind == StageJourneyStepKind.Query && table.TryGetValue(simple, out route!))) return true;
        var qualified = table.Keys.Where(key => key.EndsWith($".{simple}", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        var diagnostic = routes.Diagnostics.FirstOrDefault(entry => entry.StartsWith($"{method} '{simple}' ", StringComparison.Ordinal));
        problem = qualified.Length > 1
            ? $"{kind} '{step.Target}' is ambiguous between {string.Join(", ", qualified)}; use the qualified name"
            : diagnostic ?? $"no modeled {kind} is named '{step.Target}'";

        return false;
    }

    sealed record PreparedRequest(string Route, string? Body);
}

/// <summary>
/// The values a journey has bound so far and the substitution of <c language="text">${name}</c>, <c language="text">$new:name</c> and date tokens.
/// </summary>
/// <param name="time">The clock the date tokens read.</param>
internal sealed partial class StageJourneyBindings(TimeProvider time)
{
    readonly Dictionary<string, JsonElement> _values = new(StringComparer.Ordinal);
    string? _problem;

    [GeneratedRegex(@"\$\{(?<name>[^}]+)\}", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Token { get; }

    [GeneratedRegex(@"^today(?:(?<sign>[+-])(?<days>[0-9]{1,5})d)?$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Day { get; }

    [GeneratedRegex(@"\.(?<property>[A-Za-z_][A-Za-z0-9_]*)|\[(?<index>[0-9]+)\]", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex PathSegment { get; }

    /// <summary>
    /// Captures each bound path from a passed step's answer.
    /// </summary>
    /// <param name="bind">The paths by binding name.</param>
    /// <param name="body">The command response or query data.</param>
    /// <param name="problem">Why a path could not be read.</param>
    /// <returns><see langword="true"/> when every path was read.</returns>
    internal bool TryCapture(IReadOnlyDictionary<string, string> bind, JsonElement? body, out string problem)
    {
        problem = string.Empty;
        foreach (var (name, path) in bind)
        {
            var root = path.StartsWith("$.response", StringComparison.Ordinal) ? "$.response".Length : "$.result".Length;
            var value = body;
            foreach (Match segment in PathSegment.Matches(path[root..]))
            {
                value = value is { } current ? Step(current, segment) : null;
            }

            if (value is not { ValueKind: not JsonValueKind.Null } found)
            {
                problem = $"bind '{name}': {path} has no value";
                return false;
            }

            _values[name] = found.Clone();
        }

        return true;
    }

    /// <summary>
    /// Substitutes the bindings in every string of a value.
    /// </summary>
    /// <param name="template">The value as written.</param>
    /// <param name="value">The substituted value.</param>
    /// <param name="problem">The first unknown binding.</param>
    /// <returns><see langword="true"/> when every binding was known.</returns>
    internal bool TrySubstitute(JsonElement template, out JsonElement value, out string problem)
    {
        _problem = null;
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            Write(writer, template);
        }

        using var document = JsonDocument.Parse(buffer.ToArray());
        value = document.RootElement.Clone();
        problem = _problem ?? string.Empty;

        return _problem is null;
    }

    /// <summary>
    /// Substitutes the bindings in every string an expectation holds.
    /// </summary>
    /// <param name="template">The expectation as written.</param>
    /// <param name="expect">The substituted expectation.</param>
    /// <param name="problem">The first unknown binding.</param>
    /// <returns><see langword="true"/> when every binding was known.</returns>
    internal bool TrySubstitute(StageJourneyExpectation template, out StageJourneyExpectation expect, out string problem)
    {
        _problem = null;
        expect = template with
        {
            Messages = [.. template.Messages.Select(Text)],
            Response = Optional(template.Response),
            Result = Optional(template.Result),
            Absent = [.. template.Absent.Select(element => Optional(element)!.Value)]
        };
        problem = _problem ?? string.Empty;

        return _problem is null;
    }

    /// <summary>
    /// Copies the values bound so far.
    /// </summary>
    /// <returns>The values by name, or <see langword="null"/> when nothing is bound.</returns>
    internal IReadOnlyDictionary<string, JsonElement>? Snapshot() => _values.Count == 0 ? null : new Dictionary<string, JsonElement>(_values, StringComparer.Ordinal);

    static JsonElement? Step(JsonElement current, Match segment)
    {
        if (segment.Groups["property"].Success) return StageJourneyMatching.Property(current, segment.Groups["property"].Value);
        if (!int.TryParse(segment.Groups["index"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var index)) return null;

        return current.ValueKind == JsonValueKind.Array && index < current.GetArrayLength() ? current[index] : null;
    }

    static string Inline(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();

    JsonElement? Optional(JsonElement? template)
    {
        if (template is not { } value) return null;
        var problem = _problem;
        var known = TrySubstitute(value, out var substituted, out var found);
        _problem = problem ?? (known ? null : found);

        return substituted;
    }

    void Write(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    Write(writer, item);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                String(element.GetString()!).WriteTo(writer);
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    JsonElement String(string text)
    {
        if (text.StartsWith("$new:", StringComparison.Ordinal) && text.Length > 5)
        {
            var name = text[5..];
            if (!_values.TryGetValue(name, out var generated))
            {
                _values[name] = generated = JsonSerializer.SerializeToElement(Guid.NewGuid().ToString());
            }

            return generated;
        }

        var whole = Token.Match(text);
        return whole.Success && whole.Length == text.Length
            ? Resolve(whole.Groups["name"].Value) ?? JsonSerializer.SerializeToElement(text)
            : JsonSerializer.SerializeToElement(Text(text));
    }

    string Text(string text) => Token.Replace(text, match => Resolve(match.Groups["name"].Value) is { } value ? Inline(value) : match.Value);

    JsonElement? Resolve(string token)
    {
        var now = time.GetUtcNow();
        if (string.Equals(token, "now", StringComparison.Ordinal)) return JsonSerializer.SerializeToElement(now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        var day = Day.Match(token);
        if (day.Success)
        {
            var days = day.Groups["days"].Success ? int.Parse(day.Groups["days"].Value, CultureInfo.InvariantCulture) : 0;
            var offset = string.Equals(day.Groups["sign"].Value, "-", StringComparison.Ordinal) ? -days : days;
            return JsonSerializer.SerializeToElement(now.UtcDateTime.Date.AddDays(offset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (_values.TryGetValue(token, out var bound)) return bound;
        _problem ??= $"unknown binding '${{{token}}}'";

        return null;
    }
}

/// <summary>
/// One classified HTTP answer.
/// </summary>
/// <param name="Kind">Whether a command or a query answered.</param>
/// <param name="HttpStatus">The HTTP status code.</param>
/// <param name="Outcome">The classified outcome.</param>
/// <param name="Messages">The validation, authorization or refusal messages.</param>
/// <param name="Body">The command response or query data.</param>
/// <param name="Error">Why the answer is not a usable outcome, or <see langword="null"/>.</param>
internal sealed record StageJourneyAnswer(StageJourneyStepKind Kind, int HttpStatus, string Outcome, IReadOnlyList<string> Messages, JsonElement? Body, string? Error = null)
{
    /// <summary>
    /// Classifies an Arc command or query answer.
    /// </summary>
    /// <param name="kind">Whether a command or a query answered.</param>
    /// <param name="response">The HTTP response.</param>
    /// <param name="text">The response body.</param>
    /// <returns>The classified answer.</returns>
    internal static StageJourneyAnswer Classify(StageJourneyStepKind kind, HttpResponseMessage response, string text)
    {
        var status = (int)response.StatusCode;
        var body = Object(text);
        if (response.Headers.TryGetValues("Stage-Unsupported-Capability", out var capability))
        {
            var messages = Strings(body, "exceptionMessages");
            return new(kind, status, StageJourneyOutcome.Unsupported, messages.Count > 0 ? messages : [.. capability], null);
        }

        if (status is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden || (body is { } refused && StageJourneyMatching.Property(refused, "isAuthorized") is { ValueKind: JsonValueKind.False }))
        {
            var reason = body is { } json ? StageJourneyMatching.Property(json, "authorizationFailureReason")?.GetString() : null;
            return new(kind, status, StageJourneyOutcome.Unauthorized, string.IsNullOrEmpty(reason) ? [] : [reason], null);
        }

        if (body is not { } answer) return new(kind, status, StageJourneyOutcome.Error, [], null, $"the answer is not a JSON object (HTTP {status})");
        if (StageJourneyMatching.Property(answer, "isSuccess") is { ValueKind: JsonValueKind.True })
        {
            return new(kind, status, StageJourneyOutcome.Accepted, [], StageJourneyMatching.Property(answer, kind == StageJourneyStepKind.Command ? "response" : "data"));
        }

        var validation = StageJourneyMatching.Property(answer, "validationResults") is { ValueKind: JsonValueKind.Array } results
            ? results.EnumerateArray().Select(result => StageJourneyMatching.Property(result, "message")?.GetString() ?? string.Empty).ToArray()
            : [];
        if (validation.Length > 0) return new(kind, status, StageJourneyOutcome.Rejected, validation, null);
        var exceptions = Strings(body, "exceptionMessages");

        return new(kind, status, StageJourneyOutcome.Error, exceptions, null, exceptions.Count > 0 ? string.Join("; ", exceptions) : $"the answer is neither a success nor a refusal (HTTP {status})");
    }

    static JsonElement? Object(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            // Not JSON: the classification reports the HTTP status instead of a body.
            return null;
        }
    }

    static List<string> Strings(JsonElement? body, string property) =>
        body is { } json && StageJourneyMatching.Property(json, property) is { ValueKind: JsonValueKind.Array } values
            ? [.. values.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()!)]
            : [];
}

/// <summary>
/// Compares a classified answer with what a step expects.
/// </summary>
internal static class StageJourneyComparison
{
    /// <summary>
    /// Compares the answer with the expectation.
    /// </summary>
    /// <param name="expect">What the step expects, with bindings substituted.</param>
    /// <param name="answer">The classified answer.</param>
    /// <returns>Every mismatch; empty when the step passes.</returns>
    internal static IReadOnlyList<StageJourneyMismatch> Compare(StageJourneyExpectation expect, StageJourneyAnswer answer)
    {
        if (expect.Outcome != answer.Outcome)
        {
            return [new("$.outcome", Json(expect.Outcome), Json(answer.Outcome), $"expected {expect.Outcome}, found {answer.Outcome}")];
        }

        var mismatches = new List<StageJourneyMismatch>();
        mismatches.AddRange(expect.Messages
            .Select((message, index) => (message, index))
            .Where(expected => !answer.Messages.Any(actual => actual.Contains(expected.message, StringComparison.OrdinalIgnoreCase)))
            .Select(expected => new StageJourneyMismatch($"$.messages[{expected.index}]", Json(expected.message), Json(answer.Messages), "no message contains the expected text")));
        if (expect.Response is { } response) mismatches.AddRange(StageJourneyMatching.Compare(response, answer.Body, expect.List, "$.response"));
        if (expect.Result is { } result) mismatches.AddRange(StageJourneyMatching.Compare(result, answer.Body, expect.List, "$.result"));
        if (expect.Empty && answer.Body is { } rows && !(rows.ValueKind == JsonValueKind.Null || (rows.ValueKind == JsonValueKind.Array && rows.GetArrayLength() == 0)))
        {
            mismatches.Add(new("$.result", Json(Array.Empty<object>()), rows, "expected no rows"));
        }

        mismatches.AddRange(Absent(expect, answer.Body));

        return mismatches;
    }

    static IEnumerable<StageJourneyMismatch> Absent(StageJourneyExpectation expect, JsonElement? body)
    {
        var rows = body switch
        {
            { ValueKind: JsonValueKind.Array } array => array.EnumerateArray().ToArray(),
            { ValueKind: not JsonValueKind.Null } single => [single],
            _ => []
        };

        return expect.Absent
            .Select((absent, index) => (absent, index, row: rows.FirstOrDefault(row => StageJourneyMatching.Matches(absent, row, expect.List))))
            .Where(found => found.row.ValueKind != JsonValueKind.Undefined)
            .Select(found => new StageJourneyMismatch($"$.absent[{found.index}]", found.absent, found.row, "a result row matches a value that must be absent"));
    }

    static JsonElement Json<T>(T value) => JsonSerializer.SerializeToElement(value);
}

/// <summary>
/// The status a replayed step reports.
/// </summary>
internal static class StageJourneyStepStatus
{
    internal const string Passed = "passed";
    internal const string Failed = "failed";
    internal const string Skipped = "skipped";
    internal const string Error = "error";
}

/// <summary>
/// The report of one replay.
/// </summary>
/// <param name="Schema">The report schema.</param>
/// <param name="Passed">Whether every step of every journey passed.</param>
/// <param name="Isolation">Where the steps ran: <c language="text">live</c> is the session's own world.</param>
/// <param name="Summary">The counts.</param>
/// <param name="Journeys">Each journey's result.</param>
internal sealed record StageJourneyReport(string Schema, bool Passed, string Isolation, StageJourneySummary Summary, IReadOnlyList<StageJourneyResult> Journeys)
{
    internal static StageJourneyReport From(IReadOnlyList<StageJourneyResult> journeys)
    {
        var steps = journeys.SelectMany(journey => journey.Steps).ToArray();
        int Count(string status) => steps.Count(step => step.Status == status);
        var summary = new StageJourneySummary(journeys.Count, journeys.Count(journey => journey.Passed), steps.Length, Count(StageJourneyStepStatus.Passed), Count(StageJourneyStepStatus.Failed), Count(StageJourneyStepStatus.Skipped), Count(StageJourneyStepStatus.Error));

        return new("stage-journey-report/v1", journeys.All(journey => journey.Passed), "live", summary, journeys);
    }
}

/// <summary>
/// The counts of a replay.
/// </summary>
/// <param name="Journeys">The journeys replayed.</param>
/// <param name="JourneysPassed">The journeys whose every step passed.</param>
/// <param name="Steps">The steps.</param>
/// <param name="StepsPassed">The steps that passed.</param>
/// <param name="StepsFailed">The steps whose answer did not match.</param>
/// <param name="StepsSkipped">The steps not run because an earlier step of their journey did not pass.</param>
/// <param name="StepsErrored">The steps that could not be executed.</param>
internal sealed record StageJourneySummary(int Journeys, int JourneysPassed, int Steps, int StepsPassed, int StepsFailed, int StepsSkipped, int StepsErrored);

/// <summary>
/// The result of one journey.
/// </summary>
/// <param name="Name">The journey name.</param>
/// <param name="Passed">Whether every step passed.</param>
/// <param name="Steps">Each step's result.</param>
internal sealed record StageJourneyResult(string Name, bool Passed, IReadOnlyList<StageJourneyStepResult> Steps)
{
    public string? Note { get; init; }

    public IReadOnlyList<string>? Evidence { get; init; }
}

/// <summary>
/// The result of one step.
/// </summary>
/// <param name="Index">The zero-based index of the step in its journey.</param>
/// <param name="Name">The step name.</param>
/// <param name="Kind"><c language="text">command</c> or <c language="text">query</c>.</param>
/// <param name="Target">The model name as written.</param>
/// <param name="Status">The step status.</param>
/// <param name="DurationMs">How long the step took.</param>
internal sealed record StageJourneyStepResult(int Index, string? Name, string Kind, string Target, string Status, long DurationMs)
{
    public string? Route { get; init; }

    public int? HttpStatus { get; init; }

    public int? Attempts { get; init; }

    public StageJourneyActual? Actual { get; init; }

    public JsonElement? Expected { get; init; }

    public IReadOnlyList<StageJourneyMismatch>? Mismatches { get; init; }

    public IReadOnlyDictionary<string, JsonElement>? Bindings { get; init; }

    public string? Note { get; init; }

    public IReadOnlyList<string>? Evidence { get; init; }
}

/// <summary>
/// What a failed step actually answered.
/// </summary>
/// <param name="Outcome">The classified outcome.</param>
/// <param name="Messages">The validation, authorization or refusal messages.</param>
internal sealed record StageJourneyActual(string Outcome, IReadOnlyList<string> Messages)
{
    public JsonElement? Response { get; init; }

    public JsonElement? Result { get; init; }

    public bool? Truncated { get; init; }
}
