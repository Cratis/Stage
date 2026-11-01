// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Kits;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Describes the components that determine what a declarative plan was rendered from.
/// </summary>
static class DeclarativeComponents
{
    internal const string EngineName = "scriban";

    internal static ImmutableArray<ArtifactRenderComponent> For(LoadedKit kit, LanguagePackVersion pack) =>
    [
        new(ArtifactRenderComponentKind.Kit, kit.Manifest.Target.Id, kit.Manifest.Target.Version, kit.Digest),
        new(ArtifactRenderComponentKind.LanguagePack, pack.Id, pack.Version),
        new(ArtifactRenderComponentKind.TemplateProfile, TemplateProfile.Id, TemplateProfile.Contract.ToString()),
        new(ArtifactRenderComponentKind.HelperCatalogue, TemplateProfile.Id, "v1", Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(HelperCatalogue.GenerateReference())))),
        new(ArtifactRenderComponentKind.TemplateEngine, EngineName, EngineVersion())
    ];

    /// <summary>
    /// Resolves the loaded engine version by assembly name, without a compile-time reference.
    /// </summary>
    /// <returns>The informational version without build metadata.</returns>
    internal static string EngineVersion()
    {
        var name = typeof(TemplateProfile).Assembly.GetReferencedAssemblies().First(_ => _.Name == "Scriban");
        var assembly = Assembly.Load(name);
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? name.Version!.ToString();
        var metadata = version.IndexOf('+', StringComparison.Ordinal);
        return metadata < 0 ? version : version[..metadata];
    }
}
