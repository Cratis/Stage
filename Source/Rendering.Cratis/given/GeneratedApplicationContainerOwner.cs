// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Who started a container: the labels written atomically at docker create.
/// </summary>
public sealed record GeneratedApplicationContainerOwner
{
    /// <summary>Gets the host the owning test process ran on.</summary>
    public required string Host { get; init; }

    /// <summary>Gets the owning process id.</summary>
    public required int ProcessId { get; init; }

    /// <summary>Gets when the owning process started, in Unix seconds. A process id alone is reused.</summary>
    public required long ProcessStartedUnixSeconds { get; init; }

    /// <summary>Gets the token that is unique to one run.</summary>
    public required string RunToken { get; init; }

    /// <summary>Gets when the container was labelled, in Unix seconds.</summary>
    public required long StartedUnixSeconds { get; init; }

    /// <summary>The label key that marks a container as belonging to this fixture family.</summary>
    public const string FamilyKey = "io.cratis.stage.harness.fixture";

    /// <summary>The label value of the fixture family.</summary>
    public const string FamilyValue = "standalone-generated-application";

    /// <summary>The label key of the run token.</summary>
    public const string RunKey = "io.cratis.stage.harness.run";

    /// <summary>The label key of the start time.</summary>
    public const string StartedKey = "io.cratis.stage.harness.started";

    /// <summary>The label key of the host.</summary>
    public const string HostKey = "io.cratis.stage.harness.host";

    /// <summary>The label key of the owning process id.</summary>
    public const string ProcessKey = "io.cratis.stage.harness.pid";

    /// <summary>The label key of the owning process start time.</summary>
    public const string ProcessStartedKey = "io.cratis.stage.harness.pid-started";

    /// <summary>
    /// Describes the current process as the owner of a new run.
    /// </summary>
    /// <param name="clock">The clock.</param>
    /// <returns>The owner.</returns>
    public static GeneratedApplicationContainerOwner ForCurrentProcess(TimeProvider clock)
    {
        using var process = Process.GetCurrentProcess();
        return new()
        {
            Host = Environment.MachineName,
            ProcessId = process.Id,
            ProcessStartedUnixSeconds = OwnerProcesses.StartedUnixSeconds(process),
            RunToken = Guid.NewGuid().ToString("N"),
            StartedUnixSeconds = clock.GetUtcNow().ToUnixTimeSeconds()
        };
    }

    /// <summary>
    /// Reads an owner from container labels.
    /// </summary>
    /// <param name="labels">The labels.</param>
    /// <param name="owner">The owner when every label is present and well-formed.</param>
    /// <returns>True when the container carries a complete owner.</returns>
    public static bool TryRead(IReadOnlyDictionary<string, string> labels, out GeneratedApplicationContainerOwner owner)
    {
        owner = null!;
        if (!TryReadIdentity(labels, out var host, out var run))
        {
            return false;
        }

        if (!TryNumber(labels, ProcessKey, out var processId) || processId is < 1 or > int.MaxValue)
        {
            return false;
        }

        if (!TryNumber(labels, ProcessStartedKey, out var processStarted))
        {
            return false;
        }

        if (!TryNumber(labels, StartedKey, out var started))
        {
            return false;
        }

        owner = new()
        {
            Host = host,
            ProcessId = (int)processId,
            ProcessStartedUnixSeconds = processStarted,
            RunToken = run,
            StartedUnixSeconds = started
        };
        return true;
    }

    /// <summary>
    /// Gets the --label key=value arguments that record this owner at create time.
    /// </summary>
    /// <returns>The arguments.</returns>
    public IEnumerable<string> Labels()
    {
        foreach (var (key, value) in Pairs())
        {
            yield return "--label";
            yield return $"{key}={value}";
        }
    }

    static bool TryReadIdentity(IReadOnlyDictionary<string, string> labels, out string host, out string run)
    {
        run = string.Empty;
        host = string.Empty;
        return IsFamilyMember(labels) && TryText(labels, HostKey, out host) && TryRunToken(labels, out run);
    }

    static bool IsFamilyMember(IReadOnlyDictionary<string, string> labels) => labels.TryGetValue(FamilyKey, out var family) && family == FamilyValue;

    static bool TryText(IReadOnlyDictionary<string, string> labels, string key, out string value)
    {
        labels.TryGetValue(key, out var text);
        value = text ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    static bool TryRunToken(IReadOnlyDictionary<string, string> labels, out string run) => TryText(labels, RunKey, out run) && run.All(char.IsAsciiLetterOrDigit);

    static bool TryNumber(IReadOnlyDictionary<string, string> labels, string key, out long value)
    {
        value = 0;
        return labels.TryGetValue(key, out var text) && long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    IEnumerable<(string Key, string Value)> Pairs()
    {
        yield return (FamilyKey, FamilyValue);
        yield return (RunKey, RunToken);
        yield return (StartedKey, StartedUnixSeconds.ToString(CultureInfo.InvariantCulture));
        yield return (HostKey, Host);
        yield return (ProcessKey, ProcessId.ToString(CultureInfo.InvariantCulture));
        yield return (ProcessStartedKey, ProcessStartedUnixSeconds.ToString(CultureInfo.InvariantCulture));
    }
}
