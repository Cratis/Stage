// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

internal enum StageHostMode
{
    Sandbox,
    Published
}

internal static class StageHostModeSelection
{
    internal static string[] ConfigurationArguments(string[] arguments) => [.. arguments.Where(argument => argument != "--published")];

    internal static IConfiguration EarlyConfiguration(string[] arguments) => Published.StageHostModeSelection.EarlyConfiguration(arguments);

    internal static StageHostMode Read(string[] arguments, IConfiguration configuration) => SelectedMode(configuration, arguments);

    internal static StageHostMode Read(IConfiguration configuration, string[] arguments, StageRuntimeEngine engine, bool warmMode = false)
    {
        var mode = SelectedMode(configuration, arguments);
        if (mode == StageHostMode.Published)
        {
            RequirePublishedConfiguration(configuration, arguments, engine, warmMode);
        }

        return mode;
    }

    static StageHostMode SelectedMode(IConfiguration configuration, string[] arguments)
    {
        var configured = ConfiguredMode(configuration);
        if (configured == StageHostMode.Published || !arguments.Contains("--published", StringComparer.Ordinal))
        {
            return configured;
        }
        if (configuration["Stage:Mode"] is not null || configuration["STAGE_MODE"] is not null)
        {
            throw new StageHostModeRefused("--published conflicts with explicitly configured Sandbox mode.");
        }
        return StageHostMode.Published;
    }

    static StageHostMode ConfiguredMode(IConfiguration configuration) =>
        (configuration["Stage:Mode"] ?? configuration["STAGE_MODE"] ?? nameof(StageHostMode.Sandbox)).ToLowerInvariant() switch
        {
            "sandbox" => StageHostMode.Sandbox,
            "published" => StageHostMode.Published,
            _ => throw new StageHostModeRefused("Stage:Mode must be Sandbox or Published.")
        };

    static void RequirePublishedConfiguration(IConfiguration configuration, string[] arguments, StageRuntimeEngine engine, bool warmMode)
    {
        if (warmMode || IsWarm(configuration, arguments))
        {
            throw new StageHostModeRefused("Published mode does not support --warm or STAGE_WARM.");
        }
        if (engine == StageRuntimeEngine.EventModel)
        {
            throw new StageHostModeRefused("Published mode does not support --engine=eventmodel. Use the semantic engine.");
        }
        var problems = Published.PublishedOptions.From(configuration).Problems();
        if (problems.Count > 0)
        {
            throw new StageHostModeRefused(string.Join(Environment.NewLine, problems));
        }
    }

    static bool IsWarm(IConfiguration configuration, string[] arguments)
    {
        var configured = bool.TryParse(configuration["STAGE_WARM"], out var warm) && warm;
        return configured || arguments.Contains("--warm", StringComparer.Ordinal);
    }
}
