// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Represents the exact npm package versions a scaffolded Cratis application composes its frontend from.
/// </summary>
/// <remarks>
/// <para>
/// This set is deliberately a sidecar to <see cref="CratisBackendApplicationScaffoldProfile"/> rather than
/// more members on it: the profile's public static surface is frozen to its verified <c language="csharp">Current</c>
/// profile, and the frontend era moves on npm publish cadence rather than on the NuGet cadence of the backend pins.
/// </para>
/// <para>
/// Shared Cratis packages follow Source/Frontend/package.json, so generated applications use the packages Stage
/// itself verifies. Each rendered application's exact pins remain reproducible after Stage's tooling moves forward.
/// </para>
/// <para>
/// It is equally separate from the emitted <b>backend</b> pins on <see cref="CratisBackendApplicationScaffoldProfile"/>.
/// Both describe the same emitted era, but they resolve from different registries and cannot be kept in lockstep by
/// sharing a single version string: only <c language="json">@cratis/arc</c> and its siblings share the exact version with the NuGet
/// <c language="csharp">Cratis</c> packages, while <c language="json">rxjs</c>, <c language="json">tsyringe</c> and <c language="json">reflect-metadata</c> have no
/// NuGet counterpart at all.
/// </para>
/// <para>
/// The Scene family supplies shared binding resolvers, typed runtime contracts, the default blueprint, package-owned PrimeReact mapping,
/// editable keyed lookups, explicit command inputs and identity-aware bindings, with Components supplying the optional native-form footer. Frontend
/// installation and native consumer verification use those published packages as a separate gate;
/// C# rendering checks alone do not establish it.
/// </para>
/// </remarks>
public sealed class CratisFrontendPackageSet
{
    CratisFrontendPackageSet(
        string arcPackageVersion,
        string arcReactPackageVersion,
        string arcVitePackageVersion,
        string fundamentalsPackageVersion,
        string rxjsPackageVersion,
        string tsyringePackageVersion,
        string reflectMetadataPackageVersion,
        string reactPackageVersion,
        string reactDomPackageVersion,
        string reactRouterDomPackageVersion,
        string vitePackageVersion,
        string typeScriptPackageVersion,
        string vitePluginReactPackageVersion,
        string typesReactPackageVersion,
        string typesReactDomPackageVersion,
        string componentsPackageVersion,
        string scenePackageVersion,
        string primeReactPackageVersion,
        string primeIconsPackageVersion,
        string lucideReactPackageVersion,
        string primeUixThemesPackageVersion,
        string typesNodePackageVersion)
    {
        ArcPackageVersion = arcPackageVersion;
        ArcReactPackageVersion = arcReactPackageVersion;
        ArcVitePackageVersion = arcVitePackageVersion;
        FundamentalsPackageVersion = fundamentalsPackageVersion;
        RxjsPackageVersion = rxjsPackageVersion;
        TsyringePackageVersion = tsyringePackageVersion;
        ReflectMetadataPackageVersion = reflectMetadataPackageVersion;
        ReactPackageVersion = reactPackageVersion;
        ReactDomPackageVersion = reactDomPackageVersion;
        ReactRouterDomPackageVersion = reactRouterDomPackageVersion;
        VitePackageVersion = vitePackageVersion;
        TypeScriptPackageVersion = typeScriptPackageVersion;
        VitePluginReactPackageVersion = vitePluginReactPackageVersion;
        TypesReactPackageVersion = typesReactPackageVersion;
        TypesReactDomPackageVersion = typesReactDomPackageVersion;
        ComponentsPackageVersion = componentsPackageVersion;
        ScenePackageVersion = scenePackageVersion;
        PrimeReactPackageVersion = primeReactPackageVersion;
        PrimeIconsPackageVersion = primeIconsPackageVersion;
        LucideReactPackageVersion = lucideReactPackageVersion;
        PrimeUixThemesPackageVersion = primeUixThemesPackageVersion;
        TypesNodePackageVersion = typesNodePackageVersion;
    }

    /// <summary>
    /// Gets the exact <c language="json">@cratis/arc</c> package version.
    /// </summary>
    public string ArcPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">@cratis/arc.react</c> package version.
    /// </summary>
    public string ArcReactPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">@cratis/arc.vite</c> package version.
    /// </summary>
    public string ArcVitePackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">@cratis/fundamentals</c> package version.
    /// </summary>
    public string FundamentalsPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">rxjs</c> package version.
    /// </summary>
    public string RxjsPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">tsyringe</c> package version.
    /// </summary>
    public string TsyringePackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">reflect-metadata</c> package version.
    /// </summary>
    public string ReflectMetadataPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">react</c> package version.
    /// </summary>
    public string ReactPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">react-dom</c> package version.
    /// </summary>
    public string ReactDomPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">react-router-dom</c> package version.
    /// </summary>
    public string ReactRouterDomPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">vite</c> package version.
    /// </summary>
    public string VitePackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">typescript</c> package version.
    /// </summary>
    public string TypeScriptPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">@vitejs/plugin-react</c> package version.
    /// </summary>
    public string VitePluginReactPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">@types/react</c> package version.
    /// </summary>
    public string TypesReactPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">@types/react-dom</c> package version.
    /// </summary>
    public string TypesReactDomPackageVersion { get; }

    /// <summary>
    /// Gets the frontend package set belonging to the emitted era of the current scaffold profile.
    /// </summary>
    /// <summary>
    /// Gets the exact <c language="json">@cratis/components</c> package version.
    /// </summary>
    public string ComponentsPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">@cratis/scene.*</c> package version, shared by every Scene package.
    /// </summary>
    public string ScenePackageVersion { get; }

    /// <summary>
    /// Gets the exact version shared by <c language="json">primereact</c> and every <c language="json">@primereact/*</c> package.
    /// </summary>
    /// <remarks>
    /// These are peer dependencies of <c language="json">@cratis/components</c>, which a composed screen renders through. A
    /// generated application that declared the component library without them would not install cleanly.
    /// </remarks>
    public string PrimeReactPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">primeicons</c> package version.
    /// </summary>
    public string PrimeIconsPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">lucide-react</c> package version.
    /// </summary>
    public string LucideReactPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">@primeuix/themes</c> package version.
    /// </summary>
    public string PrimeUixThemesPackageVersion { get; }

    /// <summary>
    /// Gets the exact <c language="json">@types/node</c> package version.
    /// </summary>
    /// <remarks>
    /// The emitted bundler configuration resolves its own paths through <c language="json">node:url</c>, so the frontend does
    /// not type-check without these declarations. A generated application that cannot run its own build script
    /// is not finished, and the author cannot add the dependency to a file they are told not to edit.
    /// </remarks>
    public string TypesNodePackageVersion { get; }

    /// <summary>
    /// Gets the package set the current era emits.
    /// </summary>
    /// <remarks>
    /// Every package the emitted Stage frontend runtime imports is pinned to the version the Stage frontend itself
    /// builds with (<c language="shell">Source/Frontend/package.json</c>), so the runtime a generated application
    /// compiles is the runtime the live Stage was verified with. A specification compares the two.
    /// </remarks>
    internal static CratisFrontendPackageSet Current { get; } = new(
        "22.112.1",
        "22.112.1",
        "22.112.1",
        "7.25.1",
        "7.8.2",
        "4.10.0",
        "0.2.2",
        "19.3.0",
        "19.3.0",
        "7.18.4",
        "8.3.2",
        "7.0.2",
        "6.1.1",
        "19.3.0",
        "19.3.0",
        "4.29.0",
        "4.79.1",
        "11.2.0",
        "8.0.2",
        "1.52.0",
        "3.0.1",
        "26.6.4");

    /// <summary>
    /// Creates an unvalidated frontend package set for in-assembly contract verification.
    /// </summary>
    /// <param name="arcPackageVersion">The exact <c language="json">@cratis/arc</c> package version.</param>
    /// <param name="arcReactPackageVersion">The exact <c language="json">@cratis/arc.react</c> package version.</param>
    /// <param name="arcVitePackageVersion">The exact <c language="json">@cratis/arc.vite</c> package version.</param>
    /// <param name="fundamentalsPackageVersion">The exact <c language="json">@cratis/fundamentals</c> package version.</param>
    /// <param name="rxjsPackageVersion">The exact <c language="json">rxjs</c> package version.</param>
    /// <param name="tsyringePackageVersion">The exact <c language="json">tsyringe</c> package version.</param>
    /// <param name="reflectMetadataPackageVersion">The exact <c language="json">reflect-metadata</c> package version.</param>
    /// <param name="reactPackageVersion">The exact <c language="json">react</c> package version.</param>
    /// <param name="reactDomPackageVersion">The exact <c language="json">react-dom</c> package version.</param>
    /// <param name="reactRouterDomPackageVersion">The exact <c language="json">react-router-dom</c> package version.</param>
    /// <param name="vitePackageVersion">The exact <c language="json">vite</c> package version.</param>
    /// <param name="typeScriptPackageVersion">The exact <c language="json">typescript</c> package version.</param>
    /// <param name="vitePluginReactPackageVersion">The exact <c language="json">@vitejs/plugin-react</c> package version.</param>
    /// <param name="typesReactPackageVersion">The exact <c language="json">@types/react</c> package version.</param>
    /// <param name="typesReactDomPackageVersion">The exact <c language="json">@types/react-dom</c> package version.</param>
    /// <param name="componentsPackageVersion">The exact <c language="json">@cratis/components</c> package version.</param>
    /// <param name="scenePackageVersion">The exact version shared by every <c language="json">@cratis/scene.*</c> package.</param>
    /// <param name="primeReactPackageVersion">The exact version shared by the PrimeReact packages.</param>
    /// <param name="primeIconsPackageVersion">The exact <c language="json">primeicons</c> package version.</param>
    /// <param name="lucideReactPackageVersion">The exact <c language="json">lucide-react</c> package version.</param>
    /// <param name="primeUixThemesPackageVersion">The exact <c language="json">@primeuix/themes</c> package version.</param>
    /// <param name="typesNodePackageVersion">The exact <c language="json">@types/node</c> package version.</param>
    /// <returns>The frontend package set, validated only when a profile is created from it.</returns>
    internal static CratisFrontendPackageSet Create(
        string arcPackageVersion,
        string arcReactPackageVersion,
        string arcVitePackageVersion,
        string fundamentalsPackageVersion,
        string rxjsPackageVersion,
        string tsyringePackageVersion,
        string reflectMetadataPackageVersion,
        string reactPackageVersion,
        string reactDomPackageVersion,
        string reactRouterDomPackageVersion,
        string vitePackageVersion,
        string typeScriptPackageVersion,
        string vitePluginReactPackageVersion,
        string typesReactPackageVersion,
        string typesReactDomPackageVersion,
        string componentsPackageVersion,
        string scenePackageVersion,
        string primeReactPackageVersion,
        string primeIconsPackageVersion,
        string lucideReactPackageVersion,
        string primeUixThemesPackageVersion,
        string typesNodePackageVersion) =>
        new(
            arcPackageVersion,
            arcReactPackageVersion,
            arcVitePackageVersion,
            fundamentalsPackageVersion,
            rxjsPackageVersion,
            tsyringePackageVersion,
            reflectMetadataPackageVersion,
            reactPackageVersion,
            reactDomPackageVersion,
            reactRouterDomPackageVersion,
            vitePackageVersion,
            typeScriptPackageVersion,
            vitePluginReactPackageVersion,
            typesReactPackageVersion,
            typesReactDomPackageVersion,
            componentsPackageVersion,
            scenePackageVersion,
            primeReactPackageVersion,
            primeIconsPackageVersion,
            lucideReactPackageVersion,
            primeUixThemesPackageVersion,
            typesNodePackageVersion);

    /// <summary>
    /// Gets every version in the set, for validation through the single exactness rule the scaffold profile owns.
    /// </summary>
    /// <returns>The exact versions in declaration order.</returns>
    internal IEnumerable<string> Versions()
    {
        yield return ArcPackageVersion;
        yield return ArcReactPackageVersion;
        yield return ArcVitePackageVersion;
        yield return FundamentalsPackageVersion;
        yield return RxjsPackageVersion;
        yield return TsyringePackageVersion;
        yield return ReflectMetadataPackageVersion;
        yield return ReactPackageVersion;
        yield return ReactDomPackageVersion;
        yield return ReactRouterDomPackageVersion;
        yield return VitePackageVersion;
        yield return TypeScriptPackageVersion;
        yield return VitePluginReactPackageVersion;
        yield return TypesReactPackageVersion;
        yield return TypesReactDomPackageVersion;
        yield return ComponentsPackageVersion;
        yield return ScenePackageVersion;
        yield return PrimeReactPackageVersion;
        yield return PrimeIconsPackageVersion;
        yield return LucideReactPackageVersion;
        yield return PrimeUixThemesPackageVersion;
        yield return TypesNodePackageVersion;
    }
}
