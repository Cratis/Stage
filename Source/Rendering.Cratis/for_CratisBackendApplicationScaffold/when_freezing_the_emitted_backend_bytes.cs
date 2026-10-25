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
    // Re-frozen for #783: renderer-owned globals, project compile membership and outside-namespace imports.
    const string FrozenDigest = "edee88e4184eebe025aca3909b13e8a5b9698e6f7ccc9235a33cbb44b3ff65a4";

    const string FrozenRepositoryRootDigest = "807128f2d6443da48fe273bde3cd64da14c28f35e85bd2d42818b7f062119fe1";

    string _digest = null!;

    void Because() => _digest = Digest(_first);

    [Fact] void should_change_only_scaffold_imports_from_the_previous_backend() => Digest(_first, beforePlacement: true).ShouldEqual("19ce148c0d3128edae958d34b45dbfb68dd4d19cfe45277ca24d273dd8cf2779");
    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_emit_the_frozen_repository_root_bytes() => Digest(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot())).ShouldEqual(FrozenRepositoryRootDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(_first, previousPins: true).ShouldEqual("8100938a3df827b3da55c7fbf560eff871ccee2333e3c2aeca253902e9ccea5a");

    static string Digest(ImmutableArray<ArtifactRenderInput> inputs, bool previousPins = false, bool beforePlacement = false)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var previousImports = beforePlacement || previousPins;
        foreach (var input in inputs)
        {
            if (previousImports && QualifiedCommonOutput.IsGlobals(PathOf(input))) continue;
            hash.AppendData(Encoding.UTF8.GetBytes(input.Name));
            var text = beforePlacement || previousPins ? QualifiedCommonOutput.BeforePlacement(Text(input)) : Text(input);
            hash.AppendData(Encoding.UTF8.GetBytes(FrozenScaffoldPins.Normalize(input.Name, text, previousPins)));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
