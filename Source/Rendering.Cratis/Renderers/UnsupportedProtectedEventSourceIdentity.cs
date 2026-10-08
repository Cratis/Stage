// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// The exception that is thrown when a protected concept is used as an event-source identity.
/// </summary>
/// <param name="conceptName">The authored concept name.</param>
/// <param name="attributeName">The protected-data attribute.</param>
/// <param name="identityUse">The identity declaration using the concept.</param>
/// <param name="location">The original source location of that declaration.</param>
public sealed class UnsupportedProtectedEventSourceIdentity(
    string conceptName,
    string attributeName,
    string identityUse,
    SourceLocation location) : Exception(
        $"{DiagnosticCode}: Concept '{conceptName}' with '@{attributeName}' cannot be used as {identityUse} at {location}. " +
        "Protected values are not supported as event-source identities by the Cratis renderer. " +
        "Use a surrogate Uuid identifier and carry the protected value as a separate property.")
{
    /// <summary>
    /// The stable diagnostic code for a protected event-source identity.
    /// </summary>
    public const string DiagnosticCode = "STAGE-CRATIS-COMPLIANCE-001";

    /// <summary>
    /// Gets the authored concept name.
    /// </summary>
    public string ConceptName { get; } = conceptName;

    /// <summary>
    /// Gets the protected-data attribute.
    /// </summary>
    public string AttributeName { get; } = attributeName;

    /// <summary>
    /// Gets the identity declaration using the concept.
    /// </summary>
    public string IdentityUse { get; } = identityUse;

    /// <summary>
    /// Gets the original source location of the identity declaration.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
