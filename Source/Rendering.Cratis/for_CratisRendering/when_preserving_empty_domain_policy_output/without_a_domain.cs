// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_preserving_empty_domain_policy_output;

public class without_a_domain : given.a_domain_policy_plan
{
    // SHA-256 of the matched pre-shortening raw corpus, not a dependency on its external export directory.
    static readonly Dictionary<string, string> _oldRawHashes = new(StringComparer.Ordinal)
    {
        ["Billing/Invoicing/Issue/Issue.cs"] = "E06F05267ADAF055198F61D6490E2D56D98497FB516A31A97A6B06AE28C4FB01",
        ["Billing/Invoicing/Lookup/Lookup.cs"] = "25C1F71D754F1E9109A39984F24A307A7A77E4700C8EE569781FDD2B479E8720",
        ["Common/InvoiceId.cs"] = "48B7CBC08F6CC73F578CD60348D5EE3F40439958C163BB544C04A5A9BB87A7D7",
        ["GeneratedPolicies/CustomAndStaffPolicy.cs"] = "06F16654155FCAA82A5A3F2682944E465ED1EB26586E167B06635ADCBE10193C",
        ["GeneratedPolicies/CustomPolicy.cs"] = "5831484E030AF1BF941A4B319187DF8F66F0113848547CCE0D2B6770589BC872",
        ["GeneratedPolicies/Policies.cs"] = "8DA393D01D52A652872B8F6792EE331E3F79023F09ECF21CA0882DDD3181A203",
        ["GeneratedPolicies/PolicyBodies.cs"] = "49D87A004918A1E564EDC98124F641AFB7659C95D570D837E94FCF3A4B5F10EA",
        ["GeneratedPolicies/PolicyBodies_844D6619645B0916.cs"] = "10D86F000E0F8261514AF15F11623F5814CB7F263AD59D1D59C973C2BFFB40AE",
        ["GeneratedPolicies/PolicyBodies_C13C6DD636115D6A.cs"] = "F454590E9DC9FD70434D5F9A77BC7FECF0B7748B09234296F311C1A9D1AB763D",
        ["GeneratedPolicies/StaffPolicy.cs"] = "44A83CDECBB3DDA179485476B949322A4F5BB2AEE58888996554548D45CF8AE3",
        ["GeneratedTenancy/PortableTenantValues.cs"] = "5A25F1D0A357CEDF31FADFDA45A2F2FC9F3C8BCD5D3CE6AAF0C32DFC20158AD5",
        ["Orders/Ordering/PlaceOrder/PlaceOrder.cs"] = "1F1E7B9B431FFF207A200477B21AF9720E7019EBF02BAD7E82851E6CA50803AC",
        ["Orders/Ordering/PlaceOrder/when_denied_order.cs"] = "9D5B5CEB8938B4A9D5CB1E2F5F737CA3CD389421C8EC4FC9E05811D9D139E016",
        ["Orders/Ordering/Totals/Fold.cs"] = "30105458D13DC42F707A46DB15912D9BA0BA1AEB7111D3555B4FE5F41E6A6450",
        ["Orders/Ordering/Totals/Totals.cs"] = "017DB11CD90D5B229517693ED990D43E6ECFC05571A41B2E26989D5711B509F3",
        ["TypedContexts/PolicyContext.cs"] = "FF9F71B76FEC280E43EF6501BD788D25F81CBB3249BE4E6B494E942C850A1A8A",
        ["TypedContexts/TenantId.cs"] = "D24AA192C5AFC27CA4A2660FEEEF8FC5146D2643E3B0FE5620C94443291DB23D",
        ["TypedContexts/TypedContext_31069AC315900585.cs"] = "C36D6D6E8ED3F85D5CAE12FE2F0A159D86D2A6469A0927D3041F1957E0DF0AD5",
        ["TypedContexts/TypedContext_844D6619645B0916.cs"] = "8A8D56827051DA323D6838732F314E87B801CC37113F5545CF9485DA816AF219",
        ["TypedContexts/TypedContext_C13C6DD636115D6A.cs"] = "2F5AF99DD53D14CB30B4789185B36CC3A6E9A4AFA4DAC7110AE559AB0B2EABF2"
    };

    ArtifactRenderPlan _rawPlan = null!;
    string _digest = null!;
    string _qualifiedDigest = null!;

    void Because()
    {
        var result = Plan(string.Empty);
        result.Success.ShouldBeTrue();
        _rawPlan = Assert.IsType<ArtifactRenderPlan>(result.Plan);

        var qualified = QualifiedCommonOutput.Restore(result.Plan!, () => Plan(string.Empty).Plan!);

        // Restoring only the initializer identifier must reproduce both complete HEAD path/byte pins.
        // These assertions confine the updated pins below to that one generated declaration change.
        // Since #424 the output itself uses short names, so the HEAD pins are reproduced from the qualified restore.
        // Since Cratis/Stage#848 authored bodies are indented to their block; undoing only that indentation reproduces the same pins.
        var unindented = BeforeBodyIndentation(qualified).ToArray();
        Digest(BeforeInitializerRename(LegacyLicenseHeader.Restore(unindented))).ShouldEqual("0B882F2BC3D6D8157D3116BBFDA61AC3A79DFDAA00A4C1DA4ED508DCB0DBF015");

        // Restoring the former license header must reproduce the pins from before headers became optional.
        Digest(LegacyLicenseHeader.Restore(unindented)).ShouldEqual("9D2D04A6885FBC66D2A14ED794DDEE5202D65343904974FD000F86D0E4F39A70");
        _digest = Digest(result.Artifacts);
        _qualifiedDigest = Digest(qualified);
    }

    static IEnumerable<PlannedArtifact> BeforeBodyIndentation(IEnumerable<PlannedArtifact> artifacts) => artifacts.Select(artifact =>
        artifact.RelativePath == "Orders/Ordering/Totals/Fold.cs" || artifact.RelativePath.StartsWith("GeneratedPolicies/PolicyBodies_", StringComparison.Ordinal)
            ? PlannedArtifact.CreateText(artifact.RelativePath, UnindentBody(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())), artifact.Sources)
            : artifact);

    static string UnindentBody(string text) => text
        .Replace("\n        return new Total(", "\nreturn new Total(", StringComparison.Ordinal)
        .Replace("\n        return context.Identity", "\nreturn context.Identity", StringComparison.Ordinal);

    static IEnumerable<PlannedArtifact> BeforeInitializerRename(IEnumerable<PlannedArtifact> artifacts) => artifacts.Select(artifact =>
        artifact.RelativePath.StartsWith("GeneratedPolicies/", StringComparison.Ordinal) && artifact.RelativePath.EndsWith("Policy.cs", StringComparison.Ordinal)
            ? PlannedArtifact.CreateText(
                artifact.RelativePath,
                Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace("internal static void InitializeGeneratedAuthorization()", "internal static void RegisterPolicy()", StringComparison.Ordinal),
                artifact.Sources)
            : artifact);

    static string Digest(IEnumerable<PlannedArtifact> artifacts)
    {
        // Pins the unchanged payload: paths, wrappers, bodies, policies and generated specs. Managed reports have separate byte goldens.
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var artifact in artifacts.Select(BeforeAuthenticatedQueryDefault).Where(artifact => !string.Equals(artifact.RelativePath, ".stage/completeness.json", StringComparison.Ordinal) && !string.Equals(artifact.RelativePath, "UNSUPPORTED.md", StringComparison.Ordinal)).OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(artifact.RelativePath + "\n"));
            hash.AppendData(artifact.Bytes.AsSpan());
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    [Fact]
    public void should_compile_the_exact_raw_fold_wrapper_and_policy_context_bindings()
    {
        var files = _rawPlan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var registration = new CratisBackendApplicationScaffold()
            .Create(CratisBackendApplicationScaffoldRequest.Create(_options.ApplicationName, _options.ProjectName, _options.RootNamespace))
            .Select(input => CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact) ? artifact : null)
            .Single(artifact => artifact?.RelativePath == "GeneratedPolicyRegistration.cs")!;
        var assembly = RenderedOutput.Load(files.Append(new RenderedFile(registration.RelativePath, Encoding.UTF8.GetString(registration.Bytes.AsSpan()))));
        AssertReducerBinding(assembly);
        var policyContext = assembly.GetType("Invoices.TypedContexts.PolicyContextValues", throwOnError: true)!;
        var from = policyContext.GetMethod("From", BindingFlags.Public | BindingFlags.Static)!;
        Assert.Equal(typeof(System.Security.Claims.ClaimsPrincipal), Assert.Single(from.GetParameters()).ParameterType);
        var identity = assembly.GetType("Invoices.TypedContexts.Identity", throwOnError: true)!;
        Assert.Equal(typeof(ImmutableArray<>), identity.GetProperty("Roles")!.PropertyType.GetGenericTypeDefinition());
        var claims = identity.GetProperty("Claims")!.PropertyType;
        Assert.Equal(typeof(ImmutableArray<>), claims.GetGenericTypeDefinition());
        Assert.Equal(assembly.GetType("Invoices.TypedContexts.Claim", throwOnError: true), Assert.Single(claims.GetGenericArguments()));
    }

    [Fact]
    public void should_keep_the_exact_raw_namespace_alias_targets()
    {
        var policy = RawText("TypedContexts/PolicyContext.cs");
        Assert.Contains("using StageImport0 = global::System.Collections.Immutable;", policy, StringComparison.Ordinal);
        Assert.Contains("using StageImport1 = global::System.Security.Claims;", policy, StringComparison.Ordinal);
        Assert.Contains("StageImport0::ImmutableArray<string> Roles", policy, StringComparison.Ordinal);
        Assert.Contains("StageImport0::ImmutableArray<Claim> Claims", policy, StringComparison.Ordinal);
        Assert.Contains("From(StageImport1::ClaimsPrincipal principal)", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("global::System.Collections.Immutable.ImmutableArray", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("global::System.Security.Claims.ClaimsPrincipal", policy, StringComparison.Ordinal);
    }

    [Fact]
    public void should_keep_the_exact_twenty_hashed_artifact_paths() =>
        Assert.Equal(_oldRawHashes.Keys.Order(StringComparer.Ordinal), HashedArtifacts().Select(artifact => artifact.RelativePath).Order(StringComparer.Ordinal));

    [Fact]
    public void should_limit_raw_deltas_to_fold_and_policy_context()
    {
        var changedPaths = HashedArtifacts()
            .Where(artifact => Convert.ToHexString(SHA256.HashData(artifact.Bytes.AsSpan())) != _oldRawHashes[artifact.RelativePath])
            .Select(artifact => artifact.RelativePath)
            .Order(StringComparer.Ordinal);
        // Re-frozen for Cratis/Stage#848: the two policy body files change only by the body indentation.
        Assert.Equal(["GeneratedPolicies/PolicyBodies_844D6619645B0916.cs", "GeneratedPolicies/PolicyBodies_C13C6DD636115D6A.cs", "Orders/Ordering/Totals/Fold.cs", "TypedContexts/PolicyContext.cs"], changedPaths);
    }

    [Fact]
    public void should_keep_the_other_sixteen_raw_artifacts_byte_identical()
    {
        // Re-frozen for Cratis/Stage#848: sixteen of the eighteen remain; the two PolicyBodies files gained body indentation only.
        var unchanged = HashedArtifacts().Where(artifact => artifact.RelativePath is not "Orders/Ordering/Totals/Fold.cs" and not "TypedContexts/PolicyContext.cs" and not "GeneratedPolicies/PolicyBodies_844D6619645B0916.cs" and not "GeneratedPolicies/PolicyBodies_C13C6DD636115D6A.cs").ToArray();
        Assert.Equal(16, unchanged.Length);
        Assert.All(unchanged, artifact => Assert.Equal(_oldRawHashes[artifact.RelativePath], Convert.ToHexString(SHA256.HashData(artifact.Bytes.AsSpan()))));
    }

    [Fact]
    public void should_restore_the_old_fold_bytes_by_qualifying_only_the_wrapper()
    {
        const string wrapper = "TypedContext_31069AC315900585";
        var fold = RawText("Orders/Ordering/Totals/Fold.cs");
        Assert.Contains($"new {wrapper}(", fold, StringComparison.Ordinal);
        Assert.Contains($"static Total? Transition_174CFC4A({wrapper} context)", fold, StringComparison.Ordinal);
        var restored = UnindentBody(fold).Replace($"new {wrapper}(", $"new global::Invoices.TypedContexts.{wrapper}(", StringComparison.Ordinal)
            .Replace($"({wrapper} context)", $"(global::Invoices.TypedContexts.{wrapper} context)", StringComparison.Ordinal);
        Assert.Equal(_oldRawHashes["Orders/Ordering/Totals/Fold.cs"], Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(restored))));
    }

    [Fact]
    public void should_restore_the_old_policy_body_bytes_by_removing_only_the_body_indentation()
    {
        var bodies = HashedArtifacts().Where(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/PolicyBodies_", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, bodies.Length);
        Assert.All(bodies, artifact => Assert.Equal(_oldRawHashes[artifact.RelativePath], Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(UnindentBody(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))))));
    }

    [Fact]
    public void should_restore_the_old_policy_context_bytes_by_expanding_only_the_aliases()
    {
        const string aliases = "using StageImport0 = global::System.Collections.Immutable;\nusing StageImport1 = global::System.Security.Claims;\n\n";
        var policy = RawText("TypedContexts/PolicyContext.cs");
        Assert.StartsWith(aliases, policy, StringComparison.Ordinal);
        var restored = policy[aliases.Length..]
            .Replace("StageImport0::", "global::System.Collections.Immutable.", StringComparison.Ordinal)
            .Replace("StageImport1::", "global::System.Security.Claims.", StringComparison.Ordinal);
        Assert.Equal(_oldRawHashes["TypedContexts/PolicyContext.cs"], Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(restored))));
    }

    [Fact]
    public void should_spell_no_type_with_global_outside_using_directives()
    {
        var files = _rawPlan.Artifacts.Where(artifact => artifact.RelativePath == "TypedContexts/PolicyContext.cs" || artifact.RelativePath == "GeneratedPolicies/PolicyBodies.cs" || (artifact.RelativePath.StartsWith("TypedContexts/TypedContext_", StringComparison.Ordinal) && artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))).ToArray();
        Assert.NotEmpty(files);
        Assert.All(files, artifact => Assert.All(RawText(artifact.RelativePath).Split('\n').Where(line => !line.TrimStart().StartsWith("using ", StringComparison.Ordinal)), line => Assert.DoesNotContain("global::", line, StringComparison.Ordinal)));
    }

    // Preserve the historical type-shortening pins, restoring only ById's intentional #494 access-default change.
    static PlannedArtifact BeforeAuthenticatedQueryDefault(PlannedArtifact artifact) => artifact.RelativePath == "Orders/Ordering/Totals/Totals.cs"
        ? PlannedArtifact.CreateText(
            artifact.RelativePath,
            Encoding.UTF8.GetString(artifact.Bytes.AsSpan())
                .Replace("[Authorize]", "[AllowAnonymous]", StringComparison.Ordinal)
                .Replace("[global::Cratis.Arc.Authorization.AuthorizeAttribute]", "[global::Cratis.Arc.Authorization.AllowAnonymousAttribute]", StringComparison.Ordinal),
            artifact.Sources)
        : artifact;

    IEnumerable<PlannedArtifact> HashedArtifacts() => _rawPlan.Artifacts.Select(BeforeAuthenticatedQueryDefault).Where(artifact => !string.Equals(artifact.RelativePath, ".stage/completeness.json", StringComparison.Ordinal) && !string.Equals(artifact.RelativePath, "UNSUPPORTED.md", StringComparison.Ordinal));

    [Fact]
    void should_require_an_authenticated_caller_for_the_unmodeled_query_policy()
    {
        Assert.Contains("[Authorize]", RawText("Orders/Ordering/Totals/Totals.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", RawText("Orders/Ordering/Totals/Totals.cs"), StringComparison.Ordinal);
    }

    void AssertReducerBinding(Assembly assembly)
    {
        var fold = assembly.GetType("Invoices.Orders.Ordering.Totals.Fold", throwOnError: true)!;
        var transition = Assert.Single(fold.GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(method => method.Name.StartsWith("Transition_", StringComparison.Ordinal)));
        var wrapper = Assert.Single(transition.GetParameters()).ParameterType;
        Assert.Equal("Invoices.TypedContexts", wrapper.Namespace);
        Assert.Contains(_rawPlan.Artifacts, artifact => artifact.RelativePath == $"TypedContexts/{wrapper.Name}.cs");
        var text = RawText("Orders/Ordering/Totals/Fold.cs");
        Assert.Contains("using Invoices.TypedContexts;", text, StringComparison.Ordinal);
        Assert.Contains($"new {wrapper.Name}(", text, StringComparison.Ordinal);
        Assert.Contains($"{wrapper.Name} context)", text, StringComparison.Ordinal);
        Assert.DoesNotContain($"global::Invoices.TypedContexts.{wrapper.Name}", text, StringComparison.Ordinal);
        var observed = assembly.GetType("Invoices.Orders.Ordering.PlaceOrder.OrderPlaced", throwOnError: true)!;
        Assert.Equal(observed, wrapper.GetProperty("Event")!.PropertyType);
    }

    string RawText(string path) => Encoding.UTF8.GetString(_rawPlan.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());

    // Re-frozen for #424: generated C# uses short type names and using directives; again for Cratis/Stage#848: authored bodies are indented to their block.
    [Fact] void should_keep_all_paths_and_other_bytes() => _digest.ShouldEqual("7281BBD55502D73D1916990F6B6D96F9567057AAABE6CA07FC79CC5E78B2DF03");
    [Fact] void should_confine_other_changes_to_common_and_using_placement() => _qualifiedDigest.ShouldEqual("353B73D5BBCC2AA7CC0F8090D3FD2B01A35274305ACF7B96FFB97552750C6EEF");
}
#endif
