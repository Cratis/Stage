// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_InlineCode;

public class when_verifying_original_bytes : Specification
{
    enum MissingPart
    {
        Role,
        Owner,
        Member,
        Language,
        Requirement
    }

    [Theory]
    [InlineData("return 1;\n")]
    [InlineData("return 1;\r\n")]
    [InlineData("return 1;")]
    [InlineData("return \"é\";\n")]
    void should_hash_original_bytes_before_presentation(string source)
    {
        var bytes = Encoding.UTF8.GetBytes(source);
        var result = InlineCode.Verify(bytes, Envelope(bytes));
        Assert.Empty(result.Diagnostics);
        result.Value!.ContentHash.ShouldEqual(Hash(bytes));
        result.Value.Text.ShouldEqual(source);
        result.Value.ByteSource.ShouldEqual("supplied-bytes");
    }

    [Fact]
    void should_keep_bom_in_hash_but_out_of_body()
    {
        byte[] bytes = [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes("return 1;\r\n")];
        var code = InlineCode.Verify(bytes, Envelope(bytes)).Value!;
        code.Text.ShouldEqual("return 1;\r\n");
        code.ContentHash.ShouldEqual(Hash(bytes));
        Assert.NotEqual(Hash(Encoding.UTF8.GetBytes(code.Text)), code.ContentHash);
    }

    [Fact]
    void should_snapshot_mutable_input_and_label_string_provenance()
    {
        var bytes = Encoding.UTF8.GetBytes("return 1;");
        var code = InlineCode.Verify(bytes, Envelope(bytes)).Value!;
        bytes[0] = (byte)'X';
        code.Text.ShouldEqual("return 1;");
        code.BodyLines.Single().ShouldEqual("return 1;");
        InlineCode.VerifyUtf8Text(code.Text, code.Envelope).Value!.ByteSource.ShouldEqual("utf8-string");
    }

    [Fact]
    void should_refuse_changed_hash_and_malformed_utf8()
    {
        var bytes = Encoding.UTF8.GetBytes("return 1;\n");
        Assert.Null(InlineCode.Verify(bytes, Envelope(Encoding.UTF8.GetBytes("return 1;\r\n"))).Value);
        byte[] invalid = [0xc3, 0x28];
        Assert.Null(InlineCode.Verify(invalid, Envelope(invalid)).Value);
        Assert.Null(InlineCode.VerifyUtf8Text("\ud800", Envelope(bytes)).Value);
    }

    [Theory]
    [InlineData(MissingPart.Role)]
    [InlineData(MissingPart.Owner)]
    [InlineData(MissingPart.Member)]
    [InlineData(MissingPart.Language)]
    [InlineData(MissingPart.Requirement)]
    void should_refuse_invalid_envelope(MissingPart missing)
    {
        var bytes = Encoding.UTF8.GetBytes("return 1;");
        var envelope = missing switch
        {
            MissingPart.Role => Envelope(bytes) with { Placement = Site with { Role = string.Empty } },
            MissingPart.Owner => Envelope(bytes) with { Placement = Site with { Owner = string.Empty } },
            MissingPart.Member => Envelope(bytes) with { Placement = Site with { Member = string.Empty } },
            MissingPart.Language => Envelope(bytes) with { Language = string.Empty },
            _ => Envelope(bytes) with { RequirementId = string.Empty }
        };
        var result = InlineCode.Verify(bytes, envelope);
        Assert.Null(result.Value);
        Assert.NotEmpty(result.Diagnostics);
    }

    internal static InlineCodePlacement Site => new("command", "owner", "member");
    internal static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    internal static InlineCode Code(string text, string language = "csharp", InlineCodePlacement? placement = null, string requirement = "requirement") =>
        InlineCode.VerifyUtf8Text(text, new(Hash(Encoding.UTF8.GetBytes(text)), language, placement ?? Site, requirement)).Value!;
    static InlineCodeEnvelope Envelope(byte[] bytes) => new(Hash(bytes), "csharp", Site, "requirement");
}
