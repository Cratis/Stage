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
    const string FrozenDigest = "81d9320288fc748822d7ade1864505ba577a65ee0bfa4be1e810046e19bc3a62";

    string _digest = null!;

    void Because() => _digest = Digest(_first);

    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(_first, previousPins: true).ShouldEqual("8be112259fe445493964fed0fe105fd88f36111c8bc25ed99a940ba63ec13515");

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

    // The previous frozen backend predates the Chronicle 19.66.0 and Arc 22.100.1 pins; only the generated backend project
    // previously carried the scaffold's Arc pin. Central package management now keeps all pins in Directory.Packages.props.
    static string PreviousPins(string name, string text)
    {
        var previous = text.Replace("19.66.0", "19.55.2", StringComparison.Ordinal);

        return name.EndsWith("Directory.Packages.props", StringComparison.Ordinal)
            ? previous.Replace("22.100.1", "22.87.0", StringComparison.Ordinal)
            : previous;
    }
}
