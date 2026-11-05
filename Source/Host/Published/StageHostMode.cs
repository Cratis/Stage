// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Published;

/// <summary>
/// How a Stage host presents the application it runs.
/// </summary>
internal enum StageHostMode
{
    /// <summary>
    /// The play sandbox: in-memory storage, simulated personas and every tooling surface. The default.
    /// </summary>
    Sandbox,

    /// <summary>
    /// A published application for real users: persistent storage, a trusted sign-in proxy and no tooling surfaces.
    /// </summary>
    Published
}

internal static class StageHostModeSelection
{
    internal const string ModeKey = "Stage:Mode";
    internal const string PublishedArgument = "--published";

    /// <summary>
    /// Reads the mode from the <c language="shell">--published</c> argument or <c language="shell">Stage:Mode</c>. Sandbox is the default.
    /// </summary>
    /// <param name="args">The command line.</param>
    /// <param name="configuration">The configuration the host will run with.</param>
    /// <returns>The selected mode.</returns>
    /// <exception cref="PublishedModeRefused">The configured mode is not one of Sandbox or Published.</exception>
    internal static StageHostMode Read(string[] args, IConfiguration configuration)
    {
        if (args.Contains(PublishedArgument, StringComparer.Ordinal))
        {
            return StageHostMode.Published;
        }

        var configured = configuration[ModeKey];
        if (string.IsNullOrWhiteSpace(configured))
        {
            return StageHostMode.Sandbox;
        }

        return Enum.TryParse<StageHostMode>(configured.Trim(), ignoreCase: true, out var mode)
            ? mode
            : throw new PublishedModeRefused([$"Stage:Mode '{configured}' is not supported. Use Sandbox or Published."]);
    }

    /// <summary>
    /// Builds the configuration that decides the mode before the web application exists: the optional Stage
    /// configuration file, environment variables and the command line, in the order the host itself applies them.
    /// </summary>
    /// <param name="args">The command line.</param>
    /// <returns>The early configuration.</returns>
    internal static IConfiguration EarlyConfiguration(string[] args) => new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddJsonFile(
            Environment.GetEnvironmentVariable("STAGE_CONFIG") is { Length: > 0 } configuredPath
                ? configuredPath
                : Path.Combine(AppContext.BaseDirectory, "cratis-stage.json"),
            optional: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();
}
