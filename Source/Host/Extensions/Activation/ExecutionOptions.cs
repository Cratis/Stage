// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// How the deployment runs extension backends, bound to <c lang="text">Stage:Extensions:Execution</c>. The deployment, not the extension, decides what shares a host.
/// </summary>
public sealed class ExecutionOptions
{
    /// <summary>
    /// Gets the configuration section, below <see cref="ExtensionOptions.Section"/>.
    /// </summary>
    public const string Section = "Execution";

    /// <summary>
    /// Gets or sets how long an on-demand backend may be idle before it stops, when neither the extension's contract nor the system states it.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets how often idle backends are looked for and always-on backends are checked.
    /// </summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets how long a backend may take to start accepting calls.
    /// </summary>
    public TimeSpan StartTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets how long a backend may take to stop before it is killed.
    /// </summary>
    public TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the address backends listen on. It is loopback unless a backend runs in a container or on another machine.
    /// </summary>
    public string ListenAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets where a backend is unpacked to run. Empty means the <c lang="text">backends</c> folder of the package cache.
    /// </summary>
    public string BackendsDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets which extensions may share a runtime host. An extension that is not listed gets a container of its own.
    /// </summary>
    public SharingOptions Sharing { get; set; } = new();

    /// <summary>
    /// Gets or sets the shared host of each runtime, by runtime name such as <c lang="text">dotnet</c>.
    /// </summary>
    public IDictionary<string, SharedHostOptions> Hosts { get; set; } = new Dictionary<string, SharedHostOptions>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the container of each system that runs in one, by system name.
    /// </summary>
    public IDictionary<string, ContainerOptions> Containers { get; set; } = new Dictionary<string, ContainerOptions>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Which extensions the deployment trusts to share a runtime host. A pattern is a value or a prefix ending in <c lang="text">*</c>.
/// </summary>
public sealed class SharingOptions
{
    /// <summary>
    /// Gets or sets the publishers whose extensions may share a host.
    /// </summary>
    public string[] Publishers { get; set; } = [];

    /// <summary>
    /// Gets or sets the resolved package references that may share a host.
    /// </summary>
    public string[] References { get; set; } = [];
}

/// <summary>
/// The shared host of one runtime. It is one process that loads many extensions and isolates them, and Stage talks to it as described in the execution tiers reference.
/// </summary>
public sealed class SharedHostOptions
{
    /// <summary>
    /// Gets or sets the executable that starts the host.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments of the host.
    /// </summary>
    public string[] Arguments { get; set; } = [];
}

/// <summary>
/// The container of one system.
/// </summary>
public sealed class ContainerOptions
{
    /// <summary>
    /// Gets or sets the image to run.
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container client. It must accept the <c lang="text">docker run</c> and <c lang="text">docker stop</c> command lines.
    /// </summary>
    public string Command { get; set; } = "docker";

    /// <summary>
    /// Gets or sets arguments added to <c lang="text">run</c>, before the image, such as a network or resource limit.
    /// </summary>
    public string[] Arguments { get; set; } = [];
}
