// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Defines application-level names, an optional caller profile, and domain placement for a source plan.
/// </summary>
/// <param name="ApplicationName">The application identity used for compilation and persistent stores.</param>
/// <param name="ProjectName">The application project and solution file name.</param>
/// <param name="RootNamespace">The application C# root namespace.</param>
public sealed record CratisPlanOptions(string ApplicationName, string ProjectName, string RootNamespace)
{
    /// <summary>
    /// Gets the domain sub-path, such as Sales or Sales/Retail. Empty preserves existing placement.
    /// </summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>
    /// Gets the requested gap policy. Refuse is the default. This option has no effect until the target supports gap rendering.
    /// </summary>
    public ImplementationGapMode ImplementationGaps { get; init; } = ImplementationGapMode.Refuse;

    /// <summary>
    /// Gets the refusal policy. RenderAround permits only supported specification omissions.
    /// </summary>
    public ArtifactRefusalMode Refusals { get; init; } = ArtifactRefusalMode.Refuse;

    /// <summary>
    /// Gets a value indicating whether the application root is the repository root. When true, the first-run scaffold
    /// also contains the repository's continuous integration files in the .github folder; the default adds none.
    /// The caller decides, because planning never inspects the file system.
    /// </summary>
    public bool RepositoryRoot { get; init; }

    /// <summary>
    /// Gets the plain-text lines every generated code file starts with. Empty, the default, writes no header at all.
    /// Stage writes each line as a comment in the file's language (<c language="csharp">// line</c> for C#, TypeScript and
    /// JavaScript; an empty line becomes <c language="csharp">//</c>), followed by one blank line. Lines carry no comment
    /// markers and no line breaks. Configuration, markup and documentation files never get a header.
    /// </summary>
    public ImmutableArray<string> FileHeader { get; init; } = [];

    /// <summary>
    /// Gets the modules, features or slices, beyond the plan's own selection, whose generated code - and with it their
    /// Arc proxies - is already present in the application, or null, the default, for a plan without shared UI.
    /// When set, a scoped plan also emits the application's shared UI (scene.json, src/bindings.ts, src/stage-scene.json
    /// and src/stage.ts) composed over exactly these slices and the plan's own, so
    /// the application has a working frontend after every generation without importing a proxy that does not exist.
    /// An empty selection composes over the plan's own slices only. Pass the same set to every plan of the same
    /// application state; the shared UI is deterministic for a given model and set.
    /// </summary>
    public PlanSelection? UserInterface { get; init; }

    /// <summary>
    /// Gets the slices whose generated code is already present under the application's other domain folders, so the
    /// shared UI of an application that is generated one domain at a time composes the UI of every domain. Empty, the
    /// default, means every present slice lives under this plan's <see cref="Domain"/>, as named by <see cref="UserInterface"/>.
    /// Requires <see cref="UserInterface"/>. Each domain is named once and differs from this plan's domain, and a slice is
    /// present under one domain only; a slice this plan generates is always placed under this plan's domain.
    /// Pass the same domains to every plan of the same application state: the shared UI is then the same whichever
    /// domain is generated.
    /// </summary>
    public ImmutableArray<UserInterfaceDomain> UserInterfaceDomains { get; init; } = [];

    /// <summary>
    /// Gets the caller's complete Cratis profile for source and scaffold planning, or null to use the default profile.
    /// The profile must match the application names and omit domain placement, which is owned by <see cref="Domain"/>.
    /// Scaffold-only planning includes its strings catalog but reports Scene output as application-scope only.
    /// </summary>
    public ArtifactRenderProfile? Profile { get; init; }
}
