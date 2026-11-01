// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Published;

/// <summary>
/// The manifest at the root of an exported application package. Property order is the serialized order.
/// </summary>
/// <param name="SchemaVersion">The version of this manifest layout.</param>
/// <param name="ApplicationName">The name of the application as the model declares it.</param>
/// <param name="PublishedName">The host label the application is published under, as in <c language="shell">https://&lt;publishedName&gt;.cratis.app</c>.</param>
/// <param name="ApplicationVersion">The semantic version of this package of the application.</param>
/// <param name="StageRuntimeVersion">The Stage runtime version the package requires.</param>
/// <param name="PublicationId">The identifier of the publication this package belongs to.</param>
/// <param name="ModelDigest">The digest of the model files in the package, as <c language="shell">sha256:&lt;hex&gt;</c>.</param>
/// <param name="NotesEnabled">Whether the package asks for the Notes backend.</param>
/// <param name="Authentication">What callers must be able to do for the model's policies to be satisfiable.</param>
/// <param name="ModelReferences">The identifiers the source model gave to modules, features, slices and screens. Empty when none were supplied.</param>
internal sealed record PackageManifest(
    int SchemaVersion,
    string ApplicationName,
    string PublishedName,
    string ApplicationVersion,
    string StageRuntimeVersion,
    string PublicationId,
    string ModelDigest,
    bool NotesEnabled,
    AuthenticationRequirements Authentication,
    IReadOnlyList<ModelReference> ModelReferences)
{
    internal const int CurrentSchemaVersion = 1;
    internal const string FileName = "manifest.json";
    internal const string ModelFolder = "model";
}

/// <summary>
/// The sign-in requirements derived from the model's policies.
/// </summary>
/// <param name="Required">Whether any operation requires a signed-in caller.</param>
/// <param name="Policies">The policies applied to operations that require a caller, sorted by name.</param>
/// <param name="Roles">The roles those policies mention, sorted.</param>
/// <param name="Claims">The claims those policies mention, sorted.</param>
/// <param name="OpaquePolicies">Whether any policy is a code body the provider cannot inspect.</param>
internal sealed record AuthenticationRequirements(
    bool Required,
    IReadOnlyList<string> Policies,
    IReadOnlyList<string> Roles,
    IReadOnlyList<ClaimRequirement> Claims,
    bool OpaquePolicies);

/// <summary>
/// A claim a policy asks for.
/// </summary>
/// <param name="Name">The claim name.</param>
/// <param name="Value">The required value, or <see langword="null"/> when any value satisfies it.</param>
internal sealed record ClaimRequirement(string Name, string? Value);

/// <summary>
/// The identifier the source model gave to one element of the application.
/// </summary>
/// <param name="Kind">One of <c language="shell">module</c>, <c language="shell">feature</c>, <c language="shell">slice</c> or <c language="shell">screen</c>.</param>
/// <param name="Name">The dot-separated qualified name.</param>
/// <param name="Id">The identifier supplied by the source model.</param>
internal sealed record ModelReference(string Kind, string Name, string Id);
