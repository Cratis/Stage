// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_PlannedArtifact.when_creating_verbatim_text;

public class and_bytes_contain_crlf : Specification
{
    ImmutableArray<byte> _bytes;
    PlannedArtifact _artifact = null!;

    void Establish() => _bytes = [.. Encoding.UTF8.GetBytes("a\r\nb\nc\rd")];

    void Because() => _artifact = PlannedArtifact.CreateVerbatimText("a/b.txt", _bytes);

    [Fact] void should_keep_the_bytes_unchanged() => _artifact.Bytes.ShouldEqual(_bytes);
    [Fact] void should_hash_the_unchanged_bytes() => _artifact.Sha256.ShouldEqual(Convert.ToHexString(SHA256.HashData(_bytes.AsSpan())).ToLowerInvariant());
    [Fact] void should_be_a_text_artifact() => _artifact.Kind.ShouldEqual(PlannedArtifactKind.Text);
}
