// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_preserving_empty_domain_policy_output;

public class without_a_domain : given.a_domain_policy_plan
{
    string _digest = null!;
    string _qualifiedDigest = null!;

    void Because()
    {
        var result = Plan(string.Empty);
        result.Success.ShouldBeTrue();

        var qualified = QualifiedCommonOutput.Restore(_loaded.Model, result.Plan!, new("InvoiceApp", "Invoices"));

        // Preserve both the initializer-rename pins and #494's authenticated defaults after merging main's scaffold changes.
        Digest(BeforeInitializerRename(result.Artifacts)).ShouldEqual("E79FE2D7A065477D198D4DE5057138BB4AABD3B55D45919F92D8B621E26BC130");
        Digest(BeforeInitializerRename(qualified)).ShouldEqual("5F324E6F7B2864BF1CA9762D4090AAF2084EEE649B6421A836E856FCF21498B0");
        _digest = Digest(result.Artifacts);
        _qualifiedDigest = Digest(qualified);
    }

    static IEnumerable<PlannedArtifact> BeforeInitializerRename(IEnumerable<PlannedArtifact> artifacts) => artifacts.Select(artifact =>
        artifact.RelativePath.StartsWith("GeneratedPolicies/", StringComparison.Ordinal) && artifact.RelativePath.EndsWith("Policy.cs", StringComparison.Ordinal)
            ? PlannedArtifact.CreateText(
                artifact.RelativePath,
                Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace("internal static void InitializeGeneratedAuthorization()", "internal static void RegisterPolicy()", StringComparison.Ordinal),
                artifact.Sources)
            : artifact);

    static string Digest(IEnumerable<PlannedArtifact> artifacts)
    {
        // Pins the complete output, including paths, wrappers, bodies, policies and generated specs.
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var artifact in artifacts.OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(artifact.RelativePath + "\n"));
            hash.AppendData(artifact.Bytes.AsSpan());
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    [Fact] void should_keep_all_paths_and_bytes() => _digest.ShouldEqual("E1B2A5CD84402EC04260A80044BFA676A903A30566679981EF93FEC6124F44DB");
    [Fact] void should_confine_changes_to_common_and_using_placement() => _qualifiedDigest.ShouldEqual("FCB4564727AA3469EB8462D046617AF4F0EF30B79980E38683E441EF4E615024");
}
#endif
