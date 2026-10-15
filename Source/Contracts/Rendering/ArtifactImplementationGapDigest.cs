// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactImplementationGapDigest
{
    internal static ImmutableArray<ArtifactImplementationGap> Normalize(ImmutableArray<ArtifactImplementationGap> gaps)
    {
        if (gaps.IsDefault || gaps.Any(gap => gap is null))
        {
            throw new InvalidArtifactRenderContract("Implementation gaps must be a non-default collection without null entries.");
        }

        var normalized = gaps.Select(gap => gap.Normalize()).OrderBy(gap => gap.RequirementId, StringComparer.Ordinal).ToImmutableArray();
        if (normalized.Select(gap => gap.RequirementId).Distinct(StringComparer.Ordinal).Count() != normalized.Length)
        {
            throw new InvalidArtifactRenderContract("An artifact render plan contains duplicate implementation requirement identities.");
        }

        foreach (var file in normalized.SelectMany(gap => gap.Stubs).GroupBy(stub => stub.RelativePath))
        {
            var end = 0;
            foreach (var stub in file.OrderBy(stub => stub.BodyLocator.Start))
            {
                if (stub.BodyLocator.Start < end)
                {
                    throw new InvalidArtifactRenderContract($"Implementation stub bodies overlap in '{file.Key}'.");
                }
                end = stub.BodyLocator.Start + stub.BodyLocator.Length;
            }
        }

        return normalized;
    }

    internal static void Write(BinaryWriter writer, ImmutableArray<ArtifactImplementationGap> gaps)
    {
        var normalized = Normalize(gaps);
        if (normalized.IsEmpty)
        {
            return;
        }

        writer.Write("implementation-gaps-v1");
        writer.Write(normalized.Length);
        foreach (var gap in normalized)
        {
            writer.Write((int)gap.Origin);
            writer.Write(gap.RequirementId);
            writer.Write(gap.Owner);
            writer.Write(gap.Role.ToString());
            WriteStrings(writer, gap.Hints);
            writer.Write(gap.ContextVersion);
            writer.Write(gap.ResultVersion);
            writer.Write(gap.Stubs.Length);
            foreach (var stub in gap.Stubs) WriteStub(writer, stub);
        }
    }

    static void WriteStub(BinaryWriter writer, ArtifactImplementationStub stub)
    {
        writer.Write(stub.RelativePath);
        writer.Write(stub.TypeName);
        writer.Write(stub.MemberName);
        writer.Write(stub.Signature);
        writer.Write(stub.BodyLocator.Start);
        writer.Write(stub.BodyLocator.Length);
        writer.Write(stub.TypedContext.TypeName);
        writer.Write(stub.TypedContext.Members.Length);
        foreach (var member in stub.TypedContext.Members)
        {
            writer.Write(member.Name);
            writer.Write(member.TypeName);
            writer.Write(member.IsRealized);
        }
        WriteStrings(writer, stub.AcceptanceSpecifications);
    }

    static void WriteStrings(BinaryWriter writer, ImmutableArray<string> values)
    {
        writer.Write(values.Length);
        foreach (var value in values) writer.Write(value);
    }
}
