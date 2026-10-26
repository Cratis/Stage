// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

internal sealed record InlineCodePlacement(string Role, string Owner, string Member)
{
    internal bool IsValid => !string.IsNullOrWhiteSpace(Role) && !string.IsNullOrWhiteSpace(Owner) && !string.IsNullOrWhiteSpace(Member);
}

internal sealed record InlineCodeEnvelope(string ContentHash, string Language, InlineCodePlacement Placement, string RequirementId)
{
    internal bool IsValid => Placement.IsValid && !string.IsNullOrWhiteSpace(Language) && !string.IsNullOrWhiteSpace(RequirementId);
}

internal sealed record InlineCodeDiagnostic(string Code, string Message);
internal sealed record InlineCodeVerification(InlineCode? Value, ImmutableArray<InlineCodeDiagnostic> Diagnostics);

internal sealed class InlineCode
{
    InlineCode(InlineCodeEnvelope envelope, string text, string byteSource)
    {
        Envelope = envelope;
        Text = text;
        BodyLines = [.. text.Split('\n')];
        ByteSource = byteSource;
    }

    internal InlineCodeEnvelope Envelope { get; }
    internal string Language => Envelope.Language;
    internal InlineCodePlacement Placement => Envelope.Placement;
    internal string RequirementId => Envelope.RequirementId;
    internal string ContentHash => Envelope.ContentHash;
    internal ImmutableArray<string> BodyLines { get; }
    internal string ByteSource { get; }
    internal string Text { get; }

    internal static InlineCodeVerification Verify(ReadOnlySpan<byte> bytes, InlineCodeEnvelope envelope) => VerifyCore(bytes, envelope, "supplied-bytes");

    internal static InlineCodeVerification VerifyUtf8Text(string text, InlineCodeEnvelope envelope)
    {
        try
        {
            return VerifyCore(new UTF8Encoding(false, true).GetBytes(text), envelope, "utf8-string");
        }
        catch (EncoderFallbackException)
        {
            return new(null, [new("STAGE-INLINE-INTEGRITY", "The supplied string is not valid Unicode.")]);
        }
    }

    static InlineCodeVerification VerifyCore(ReadOnlySpan<byte> bytes, InlineCodeEnvelope envelope, string byteSource)
    {
        // Snapshot before either hashing or decoding: the caller cannot mutate verified input later.
        var original = bytes.ToArray();
        var hash = Convert.ToHexStringLower(SHA256.HashData(original));
        var diagnostics = ImmutableArray.CreateBuilder<InlineCodeDiagnostic>();
        if (!string.Equals(hash, envelope.ContentHash, StringComparison.Ordinal)) diagnostics.Add(new("STAGE-INLINE-INTEGRITY", "The original-byte SHA-256 differs from ContentHash."));
        if (!envelope.IsValid) diagnostics.Add(new("STAGE-INLINE-INTEGRITY", "The inline envelope has an invalid language, requirement or placement."));
        if (diagnostics.Count > 0) return new(null, diagnostics.ToImmutable());

        try
        {
            // A UTF-8 BOM participates in the hash, but is not a statement in the presented body.
            var offset = original.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }) ? 3 : 0;
            var text = new UTF8Encoding(false, true).GetString(original.AsSpan(offset));
            return new(new(envelope, text, byteSource), []);
        }
        catch (DecoderFallbackException)
        {
            return new(null, [new("STAGE-INLINE-INTEGRITY", "The verified bytes are not valid UTF-8.")]);
        }
    }
}
