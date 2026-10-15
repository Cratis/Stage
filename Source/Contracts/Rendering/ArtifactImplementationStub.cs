// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Identifies one generated payload method whose body can be implemented independently.
/// </summary>
/// <param name="RelativePath">The artifact-relative path, never a publication destination.</param>
/// <param name="TypeName">The fully qualified declaring C# type.</param>
/// <param name="MemberName">The generated payload method name.</param>
/// <param name="Signature">The exact LF-normalized declaration text preceding the opening brace.</param>
/// <param name="BodyLocator">The body interior in the LF-normalized baseline artifact.</param>
/// <param name="TypedContext">The generated typed-context contract.</param>
/// <param name="AcceptanceSpecifications">Fully qualified, body-reaching, output-asserting specification classes. Empty means unverifiable.</param>
public sealed record ArtifactImplementationStub(
    string RelativePath,
    string TypeName,
    string MemberName,
    string Signature,
    ArtifactImplementationBodyLocator BodyLocator,
    ArtifactImplementationContext TypedContext,
    ImmutableArray<string> AcceptanceSpecifications)
{
    internal ArtifactImplementationStub Normalize()
    {
        if (string.IsNullOrWhiteSpace(TypeName) || string.IsNullOrWhiteSpace(MemberName) || string.IsNullOrWhiteSpace(Signature) ||
            Signature.Contains('\r') || BodyLocator is null || TypedContext is null || AcceptanceSpecifications.IsDefault ||
            AcceptanceSpecifications.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidArtifactRenderContract("An implementation stub requires a type, member, exact LF signature, body locator, typed context, and acceptance classes.");
        }

        BodyLocator.Validate();
        TypedContext.Validate();

        return this with
        {
            RelativePath = PlannedArtifact.NormalizePath(RelativePath),
            AcceptanceSpecifications = [.. AcceptanceSpecifications.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]
        };
    }
}
