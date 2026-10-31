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
    // Re-frozen when specifications moved into the application project, compiled in Debug only.
    // Re-frozen for #424: the scaffold places using directives outside the namespace.
    // Re-frozen when GenerateProgramFile became unconditional, so a Release build after a Debug restore keeps one entry point.
    // Re-frozen for Cratis/Stage#873
    const string FrozenDigest = "e3a60dfa26613a59f581e11b1b1403e1f624314d47125393a22b6cccb08d4849";

    // Frozen when the license header became the caller's choice: generated C# carries none by default.
    // Re-frozen for Cratis/Stage#873
    const string SeparateSpecificationsDigest = "ab43d85d047ce44fca6b0251a6cbe02b6431916d99b508ce99725213626fae03";

    // Frozen for #437: only the Matching package pin and host reference are added.
    // Re-frozen for Cratis/Stage#873
    const string PreviousFrozenDigest = "c7f6bb300ed2975b399afdcae37b288ddbccb6e5becce0c1f5852e766d8c7384";

    // Re-frozen for #424: the scaffold places using directives outside the namespace.
    // Re-frozen for Cratis/Stage#873
    const string FrozenRepositoryRootDigest = "9e4de0679881bb7b67a68e3cf0dd85972758be1b366be62a42eaae593657a4b8";

    string _digest = null!;

    void Because() => _digest = Digest(_first);

    // Re-frozen for Cratis/Stage#873
    [Fact] void should_change_only_using_placement_from_the_previous_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), beforePlacement: true).ShouldEqual("4167f8ef217c66df81d5dc1f08b5a300d15d59d4242913c49f46cad412c8d532");
    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_specifications_layout_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), legacyPlacement: true).ShouldEqual(SeparateSpecificationsDigest);
    [Fact] void should_change_only_the_license_header_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), legacyHeader: true, legacyPlacement: true).ShouldEqual(PreviousFrozenDigest);
    [Fact] void should_emit_the_frozen_repository_root_bytes() => Digest(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot())).ShouldEqual(FrozenRepositoryRootDigest);

    // Re-frozen for Cratis/Stage#873
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), previousPins: true).ShouldEqual("cf3a4cf8717c056cde9c2fd316c03d9012e62c16d275afa7a72d5616e991558e");

    static string Digest(ImmutableArray<ArtifactRenderInput> inputs, bool previousPins = false, bool beforePlacement = false, bool legacyHeader = false, bool legacyPlacement = false)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var input in inputs)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(input.Name));
            var text = beforePlacement || previousPins ? QualifiedCommonOutput.BeforePlacement(Text(input)) : Text(input);

            // Before #424 the scaffold placed the generated imports inside the namespace.
            text = legacyPlacement ? text.Replace(QualifiedCommonOutput.Placement, QualifiedCommonOutput.Placement.Replace("outside_namespace", "inside_namespace", StringComparison.Ordinal), StringComparison.Ordinal) : text;
            text = beforePlacement || previousPins || legacyHeader ? LegacyLicenseHeader.Restore(input.Name, text) : text;
            hash.AppendData(Encoding.UTF8.GetBytes(FrozenScaffoldPins.Normalize(input.Name, text, previousPins)));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
