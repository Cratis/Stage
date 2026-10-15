// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Identifies one generated payload method whose body can be implemented independently.
/// </summary>
/// <remarks>Fields added after v1 must be init properties, not additional positional parameters.</remarks>
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
        if (!HasDeclaration() || !HasBodyContract() || !HasAcceptanceClasses())
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

    internal void ValidateArtifact(IReadOnlyDictionary<string, PlannedArtifact> artifacts)
    {
        if (!artifacts.TryGetValue(RelativePath, out var artifact))
        {
            throw new InvalidArtifactRenderContract($"Implementation stub artifact '{RelativePath}' is not in the plan.");
        }
        if (!BodyFitsArtifact(artifact))
        {
            throw new InvalidArtifactRenderContract($"Implementation stub body is outside the LF-normalized text of '{RelativePath}'.");
        }
    }

    bool HasDeclaration() => HasMemberIdentity() && HasLfSignature();
    bool HasMemberIdentity() => !string.IsNullOrWhiteSpace(TypeName) && !string.IsNullOrWhiteSpace(MemberName);
    bool HasLfSignature() => !string.IsNullOrWhiteSpace(Signature) && !Signature.Contains('\r');
    bool HasBodyContract() => BodyLocator is not null && TypedContext is not null;
    bool HasAcceptanceClasses() => !AcceptanceSpecifications.IsDefault && !AcceptanceSpecifications.Any(string.IsNullOrWhiteSpace);
    bool BodyFitsArtifact(PlannedArtifact artifact) => artifact.Kind == PlannedArtifactKind.Text && BodyLocator.Start + BodyLocator.Length <= NormalizedTextLength(artifact);
    static int NormalizedTextLength(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Length;
}
