// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_freezing_the_emitted_backend_bytes : a_current_scaffold
{
    const string FrozenDigest = "44a2e890d6cb1e64c577bfb8364f4fa1e567160285dd233cbbef39f1879f1adc";

    string _digest = null!;

    void Because() => _digest = Digest(_first);

    [Fact] void should_change_only_using_placement_from_the_previous_backend() => Digest(_first, beforePlacement: true).ShouldEqual("590c38622fb70372d86a79661bb77010df19f9fa3db21ff839f7538b95095f76");
    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(_first, previousPins: true).ShouldEqual("8be112259fe445493964fed0fe105fd88f36111c8bc25ed99a940ba63ec13515");

    static string Digest(ImmutableArray<ArtifactRenderInput> inputs, bool previousPins = false, bool beforePlacement = false)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var input in inputs)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(input.Name));
            var text = beforePlacement || previousPins ? QualifiedCommonOutput.BeforePlacement(Text(input)) : Text(input);
            hash.AppendData(Encoding.UTF8.GetBytes(previousPins ? PreviousPins(input.Name, text) : text));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    // The previous frozen backend predates the Chronicle 19.71.0 and Arc 22.112.0 pins; only the generated backend project
    // previously carried the scaffold's Arc pin. Central package management now keeps all pins in Directory.Packages.props.
    static string PreviousPins(string name, string text)
    {
        var previous = text.Replace("19.71.0", "19.55.2", StringComparison.Ordinal);

        return name.EndsWith("Directory.Packages.props", StringComparison.Ordinal)
            ? previous.Replace("22.112.0", "22.87.0", StringComparison.Ordinal)
            : previous;
    }
}
