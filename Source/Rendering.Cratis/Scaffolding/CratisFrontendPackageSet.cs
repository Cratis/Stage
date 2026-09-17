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
/// It is separate from the versions Stage itself builds with. Those are Stage's own <b>tooling</b> versions - the
/// packages the Stage repository compiles, tests and ships with - and they follow whatever Stage currently develops
/// against. The versions here are an <b>emitted era</b>: the versions a generated application is pinned to, chosen so
/// the generated application stays reproducible long after Stage's own tooling has moved forward.
/// </para>
/// <para>
/// It is equally separate from the emitted <b>backend</b> pins on <see cref="CratisBackendApplicationScaffoldProfile"/>.
/// Both describe the same emitted era, but they resolve from different registries and cannot be kept in lockstep by
/// sharing a single version string: only <c language="json">@cratis/arc</c> and its siblings share the Cratis major with the NuGet
/// <c language="csharp">Cratis</c> packages, while <c language="json">rxjs</c>, <c language="json">tsyringe</c> and <c language="json">reflect-metadata</c> have no
/// NuGet counterpart at all.
/// </para>
/// <para>
/// The set is exactly the seven packages below, each resolved to the newest version published at or before the
/// publish instant of <c language="json">@cratis/arc</c> 22.3.0. It deliberately excludes <c language="json">@cratis/components</c>, which at that
/// cut drags in a full PrimeReact peer stack, and the <c language="json">@cratis/scene.*</c> packages, which existed only at 2.0.0
/// at that cut while the Scene capabilities a composed application needs shipped in 3.5.0. Selecting those belongs
/// to the step where emitted artifacts actually import them, because it may require moving the emitted era forward.
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
        string reflectMetadataPackageVersion)
    {
        ArcPackageVersion = arcPackageVersion;
        ArcReactPackageVersion = arcReactPackageVersion;
        ArcVitePackageVersion = arcVitePackageVersion;
        FundamentalsPackageVersion = fundamentalsPackageVersion;
        RxjsPackageVersion = rxjsPackageVersion;
        TsyringePackageVersion = tsyringePackageVersion;
        ReflectMetadataPackageVersion = reflectMetadataPackageVersion;
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
    /// Gets the frontend package set belonging to the emitted era of the current scaffold profile.
    /// </summary>
    internal static CratisFrontendPackageSet Current { get; } = new(
        "22.3.0",
        "22.3.0",
        "22.3.0",
        "7.18.1",
        "7.8.2",
        "4.10.0",
        "0.2.2");

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
    /// <returns>The frontend package set, validated only when a profile is created from it.</returns>
    internal static CratisFrontendPackageSet Create(
        string arcPackageVersion,
        string arcReactPackageVersion,
        string arcVitePackageVersion,
        string fundamentalsPackageVersion,
        string rxjsPackageVersion,
        string tsyringePackageVersion,
        string reflectMetadataPackageVersion) =>
        new(
            arcPackageVersion,
            arcReactPackageVersion,
            arcVitePackageVersion,
            fundamentalsPackageVersion,
            rxjsPackageVersion,
            tsyringePackageVersion,
            reflectMetadataPackageVersion);

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
    }
}
