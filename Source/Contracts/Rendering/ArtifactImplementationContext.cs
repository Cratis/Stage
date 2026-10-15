// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Describes the generated C# typed context available to a payload stub, not a runtime value.
/// </summary>
/// <param name="TypeName">The fully qualified generated context type.</param>
/// <param name="Members">The ordered context members and their realization status.</param>
public sealed record ArtifactImplementationContext(string TypeName, ImmutableArray<ArtifactImplementationContextMember> Members)
{
    internal void Validate()
    {
        if (!HasTypedMembers() || !HasUniqueMemberNames())
        {
            throw new InvalidArtifactRenderContract("An implementation context requires a type and uniquely named, typed members.");
        }
    }

    bool HasTypedMembers() => !string.IsNullOrWhiteSpace(TypeName) && HasValidMembers();
    bool HasValidMembers() => !Members.IsDefault && Members.All(IsValidMember);
    bool HasUniqueMemberNames() => Members.Select(member => member.Name).Distinct(StringComparer.Ordinal).Count() == Members.Length;
    static bool IsValidMember(ArtifactImplementationContextMember member) => member is not null && HasMemberIdentity(member);
    static bool HasMemberIdentity(ArtifactImplementationContextMember member) => !string.IsNullOrWhiteSpace(member.Name) && !string.IsNullOrWhiteSpace(member.TypeName);
}

/// <summary>
/// Describes a typed-context member without claiming unavailable runtime plumbing exists.
/// Validated through its parent <see cref="ArtifactImplementationContext"/>.
/// </summary>
/// <param name="Name">The context member name.</param>
/// <param name="TypeName">The exact C# type, including nullable and collection syntax.</param>
/// <param name="IsRealized">Whether the generated stub can actually access this member.</param>
public sealed record ArtifactImplementationContextMember(string Name, string TypeName, bool IsRealized);
