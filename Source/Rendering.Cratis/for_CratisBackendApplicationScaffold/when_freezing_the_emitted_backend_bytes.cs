// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_freezing_the_emitted_backend_bytes : a_current_scaffold
{
    const string FrozenDigest = "97953d1e5253b413ac039c1ad5f0a0d5350abe34c17e0aaa3817c51af0e7fd9b";

    string _digest = null!;

    void Because() => _digest = Digest(_first);

    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(_first, previousPins: true).ShouldEqual("a42f5d47dfa15f6b5b8b62d7e594112f384cb2781b794971f0b02e93917ab542");

    static string Digest(ImmutableArray<ArtifactRenderInput> inputs, bool previousPins = false)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var input in inputs)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(input.Name));
            hash.AppendData(previousPins
                ? Encoding.UTF8.GetBytes(PreviousPins(input.Name, Text(input)))
                : input.Bytes.AsSpan());
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    // The previous frozen backend predates the Chronicle 19.59.6 and Arc 22.88.0 pins; only the Workspaces project
    // carries the scaffold's Arc pin.
    static string PreviousPins(string name, string text)
    {
        var previous = text.Replace("19.59.6", "19.55.2", StringComparison.Ordinal);

        return name.EndsWith("Workspaces.csproj", StringComparison.Ordinal)
            ? previous.Replace("22.88.0", "22.87.0", StringComparison.Ordinal)
            : previous;
    }
}
