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
    // Re-frozen for #660: only Directory.Build.props gains CratisReadProtection On.
    // Re-frozen for #494: README documents authenticated defaults and explicit anonymous access.
    const string FrozenDigest = "89100dd8329c5c8e1f3fe53398577b3768c005900f84d5561a55d18eb4b4e6f3";

    // Frozen when the license header became the caller's choice: generated C# carries none by default.
    const string SeparateSpecificationsDigest = "57eec6d66cd8da86ec17167e4b8c913a59c9b27a68f4edac850d8f3333884735";

    // Frozen for #437: only the Matching package pin and host reference are added.
    const string PreviousFrozenDigest = "589c1e21609a0d966967440d843e40c86b3ea757648362da2072927d5465050f";

    // Re-frozen for #424: the scaffold places using directives outside the namespace.
    // Re-frozen for #660: only Directory.Build.props gains CratisReadProtection On.
    const string FrozenRepositoryRootDigest = "21e03657723006594130a7dd15e65f97cf89a3625f3154e3d7ac9b9d64044608";

    string _digest = null!;

    void Because() => _digest = Digest(_first);

    [Fact] void should_change_only_using_placement_from_the_previous_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), beforePlacement: true).ShouldEqual("cf75ed8470b566a911e8adca3639aa115cecb30355c048194ed513bb39fff14e");
    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_specifications_layout_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), legacyPlacement: true).ShouldEqual(SeparateSpecificationsDigest);
    [Fact] void should_change_only_the_license_header_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), legacyHeader: true, legacyPlacement: true).ShouldEqual(PreviousFrozenDigest);
    [Fact] void should_emit_the_frozen_repository_root_bytes() => Digest(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot())).ShouldEqual(FrozenRepositoryRootDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), previousPins: true).ShouldEqual("741bc05a794bed5fcf52ce69a334c9945da75fcb978233d16ab08b5ca3bd57df");

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

            // Before #660 the scaffold left Arc read protection at its default.
            text = ShouldRestoreLegacyReadProtection(beforePlacement, previousPins, legacyHeader, legacyPlacement) ? LegacyReadProtection.Restore(input.Name, text) : text;
            hash.AppendData(Encoding.UTF8.GetBytes(FrozenScaffoldPins.Normalize(input.Name, text, previousPins)));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    static bool ShouldRestoreLegacyReadProtection(bool beforePlacement, bool previousPins, bool legacyHeader, bool legacyPlacement) =>
        beforePlacement || previousPins || legacyHeader || legacyPlacement;
}
