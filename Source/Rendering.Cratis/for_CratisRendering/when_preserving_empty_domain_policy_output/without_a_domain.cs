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
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_preserving_empty_domain_policy_output;

public class without_a_domain : given.a_domain_policy_plan
{
    // SHA-256 of the matched pre-shortening raw corpus, not a dependency on its external export directory.
    static readonly Dictionary<string, string> _oldRawHashes = new(StringComparer.Ordinal)
    {
        ["Billing/Invoicing/Issue/Issue.cs"] = "C91EB749EF823465099E6480F958BA9001F977183127E1053BEADCFAC04CACF6",
        ["Billing/Invoicing/Lookup/Lookup.cs"] = "9423FF9F003DF886857BE90E7D39332A1E195FB3BAE79C032D116FED912AFB0F",
        ["Common/InvoiceId.cs"] = "377D50B36B874DAD1FA359C9290694768F35720A1BB593AE8947D14EC0B6408B",
        ["GeneratedPolicies/CustomAndStaffPolicy.cs"] = "06F16654155FCAA82A5A3F2682944E465ED1EB26586E167B06635ADCBE10193C",
        ["GeneratedPolicies/CustomPolicy.cs"] = "5831484E030AF1BF941A4B319187DF8F66F0113848547CCE0D2B6770589BC872",
        ["GeneratedPolicies/Policies.cs"] = "D5AA46D6A3E5B25FE3A00CF17A6AC5C19E505287417687DC8D1B066A48D04A2D",
        ["GeneratedPolicies/PolicyBodies.cs"] = "49D87A004918A1E564EDC98124F641AFB7659C95D570D837E94FCF3A4B5F10EA",
        ["GeneratedPolicies/PolicyBodies_844D6619645B0916.cs"] = "10D86F000E0F8261514AF15F11623F5814CB7F263AD59D1D59C973C2BFFB40AE",
        ["GeneratedPolicies/PolicyBodies_C13C6DD636115D6A.cs"] = "F454590E9DC9FD70434D5F9A77BC7FECF0B7748B09234296F311C1A9D1AB763D",
        ["GeneratedPolicies/StaffPolicy.cs"] = "44A83CDECBB3DDA179485476B949322A4F5BB2AEE58888996554548D45CF8AE3",
        ["GeneratedTenancy/PortableTenantValues.cs"] = "5A25F1D0A357CEDF31FADFDA45A2F2FC9F3C8BCD5D3CE6AAF0C32DFC20158AD5",
        ["Orders/Ordering/PlaceOrder/PlaceOrder.cs"] = "9AAF44FDC02647745985D5FDDD90E39A11F4A9157CE86F8C587FD58EC089537B",
        ["Orders/Ordering/PlaceOrder/when_denied_order.cs"] = "FB2ADCABE1B6E1F3E8E56348F7619FB239A897E7A6F929FFCD76010646ADFA39",
        ["Orders/Ordering/Totals/Fold.cs"] = "30105458D13DC42F707A46DB15912D9BA0BA1AEB7111D3555B4FE5F41E6A6450",
        ["Orders/Ordering/Totals/Totals.cs"] = "1BE5748D7D143575ED106A263730ACB4659D890CB0AABA93D7DCCFC587A57E47",
        ["TypedContexts/PolicyContext.cs"] = "FF9F71B76FEC280E43EF6501BD788D25F81CBB3249BE4E6B494E942C850A1A8A",
        ["TypedContexts/TenantId.cs"] = "D24AA192C5AFC27CA4A2660FEEEF8FC5146D2643E3B0FE5620C94443291DB23D",
        ["TypedContexts/TypedContext_31069AC315900585.cs"] = "C36D6D6E8ED3F85D5CAE12FE2F0A159D86D2A6469A0927D3041F1957E0DF0AD5",
        ["TypedContexts/TypedContext_844D6619645B0916.cs"] = "8A8D56827051DA323D6838732F314E87B801CC37113F5545CF9485DA816AF219",
        ["TypedContexts/TypedContext_C13C6DD636115D6A.cs"] = "2F5AF99DD53D14CB30B4789185B36CC3A6E9A4AFA4DAC7110AE559AB0B2EABF2"
    };

    ArtifactRenderPlan _rawPlan = null!;
    ImmutableArray<PlannedArtifact> _hashedArtifacts;
    string _digest = null!;
    string _qualifiedDigest = null!;
    string _restoredDigest = null!;

    void Because()
    {
        var result = Plan(string.Empty);
        result.Success.ShouldBeTrue();
        _rawPlan = Assert.IsType<ArtifactRenderPlan>(result.Plan);

        var names = new SemanticApplicationContext(
            new ArtifactRenderRequest(
                _loaded.Model,
                _loaded.Plan,
                CratisRendering.CreateProfile(_options.ApplicationName, new(_options.ProjectName, _options.RootNamespace)),
                new(ArtifactRenderScopeKind.Application, _loaded.Model.Application.Id))
            {
                ImplementationRequirements = _loaded.ImplementationRequirements,
                ImplementationContents = _loaded.ImplementationContents,
                TypedContextDescriptors = _loaded.TypedContextDescriptors
            },
            new(_options.ProjectName, _options.RootNamespace));

        // Cratis/Stage#850 made typed-context, policy-body and reducer-helper names readable; restoring the hashed spelling
        // reproduces every earlier pin, so the readable names are the only change.
        _hashedArtifacts = [.. HashedHelperNames.Restore(_rawPlan.Artifacts, names)];
        var qualified = HashedHelperNames.Restore(QualifiedCommonOutput.Restore(result.Plan!, () => Plan(string.Empty).Plan!), names).ToImmutableArray();

        // Restoring only the initializer identifier must reproduce both complete HEAD path/byte pins.
        // These assertions confine the updated pins below to that one generated declaration change.
        // Since #424 the output itself uses short names, so the HEAD pins are reproduced from the qualified restore.
        // Since Cratis/Stage#848 authored bodies are indented to their block; undoing only that indentation reproduces the same pins.
        var unindented = BeforeBodyIndentation(qualified).ToArray();
        Digest(BeforeInitializerRename(BeforeCallerIdentifier(LegacyLicenseHeader.Restore(unindented)))).ShouldEqual("CC6123922024E13204531B1E8BF9364ECCB4BD6C5E509A936457D5EC4E506753");

        // Restoring the former license header must reproduce the pins from before headers became optional.
        Digest(BeforeCallerIdentifier(LegacyLicenseHeader.Restore(unindented))).ShouldEqual("462569B15BC832AF18A1E66AFDAA7E822D92A02817123A61C2F144D506C551BD");
        _digest = Digest(result.Artifacts);
        _restoredDigest = Digest(_hashedArtifacts);
        _qualifiedDigest = Digest(qualified);
    }

    static IEnumerable<PlannedArtifact> BeforeBodyIndentation(IEnumerable<PlannedArtifact> artifacts) => artifacts.Select(artifact =>
        artifact.RelativePath == "Orders/Ordering/Totals/Fold.cs" || artifact.RelativePath.StartsWith("GeneratedPolicies/PolicyBodies_", StringComparison.Ordinal)
            ? PlannedArtifact.CreateText(artifact.RelativePath, UnindentBody(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())), artifact.Sources)
            : artifact);

    static string UnindentBody(string text) => text
        .Replace("\n        return new Total(", "\nreturn new Total(", StringComparison.Ordinal)
        .Replace("\n        return context.Identity", "\nreturn context.Identity", StringComparison.Ordinal);

    // The shared policy runtime gained the caller's identifier helper (Cratis/Stage#1061); removing exactly that block and the partial modifier
    // that carries its application hook reproduces the earlier bytes.
    static IEnumerable<PlannedArtifact> BeforeCallerIdentifier(IEnumerable<PlannedArtifact> artifacts) => artifacts.Select(artifact =>
        Encoding.UTF8.GetString(artifact.Bytes.AsSpan()) is var text && text.Contains("ResolveCallerId", StringComparison.Ordinal)
            ? PlannedArtifact.CreateText(artifact.RelativePath, RemoveCallerIdentifier(text), artifact.Sources)
            : artifact);

    // The short-named output also imports System.Security.Claims for the name identifier; nothing else in the file uses it.
    static string RemoveCallerIdentifier(string text) => System.Text.RegularExpressions.Regex.Replace(
        text.Replace("static partial class PolicyValues", "static class PolicyValues", StringComparison.Ordinal)
            .Replace("using System.Security.Claims;\n", string.Empty, StringComparison.Ordinal),
        @"[ \t]*/// <summary>\s*\n[ \t]*/// Lets an application declare.*?return string\.IsNullOrEmpty\(identifier\) \? null : identifier;\s*\n[ \t]*\}\s*\n",
        string.Empty,
        System.Text.RegularExpressions.RegexOptions.Singleline,
        TimeSpan.FromSeconds(1));

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
        foreach (var artifact in artifacts.Where(artifact => !string.Equals(artifact.RelativePath, ".stage/completeness.json", StringComparison.Ordinal) && !string.Equals(artifact.RelativePath, "UNSUPPORTED.md", StringComparison.Ordinal)).OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal))
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
            .Single(artifact => artifact?.RelativePath == "GeneratedPolicyRegistration.cs");
        var assembly = RenderedOutput.Load(files.Append(new RenderedFile(registration.RelativePath, Encoding.UTF8.GetString(registration.Bytes.AsSpan()))));
        AssertReducerBinding(assembly);
        var policyContext = assembly.GetType("Invoices.TypedContexts.PolicyContextValues", throwOnError: true);
        var from = policyContext.GetMethod("From", BindingFlags.Public | BindingFlags.Static);
        Assert.Equal(typeof(System.Security.Claims.ClaimsPrincipal), Assert.Single(from.GetParameters()).ParameterType);
        var identity = assembly.GetType("Invoices.TypedContexts.Identity", throwOnError: true);
        Assert.Equal(typeof(ImmutableArray<>), identity.GetProperty("Roles").PropertyType.GetGenericTypeDefinition());
        var claims = identity.GetProperty("Claims").PropertyType;
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
    public void should_limit_raw_deltas_to_fold_policy_context_and_type_comparisons()
    {
        var changedPaths = HashedArtifacts()
            .Where(artifact => Convert.ToHexString(SHA256.HashData(artifact.Bytes.AsSpan())) != _oldRawHashes[artifact.RelativePath])
            .Select(artifact => artifact.RelativePath)
            .Order(StringComparer.Ordinal);

        // Re-frozen for Cratis/Stage#848: the two policy body files change only by the body indentation.
        Assert.Equal(["GeneratedPolicies/Policies.cs", "GeneratedPolicies/PolicyBodies_844D6619645B0916.cs", "GeneratedPolicies/PolicyBodies_C13C6DD636115D6A.cs", "Orders/Ordering/Totals/Fold.cs", "TypedContexts/PolicyContext.cs"], changedPaths);
    }

    [Fact]
    public void should_keep_the_other_fifteen_raw_artifacts_byte_identical()
    {
        // Re-frozen for Cratis/Stage#850: Policies.cs changes only the concept-base comparisons.
        var unchanged = HashedArtifacts().Where(artifact => artifact.RelativePath is not "GeneratedPolicies/Policies.cs" and not "Orders/Ordering/Totals/Fold.cs" and not "TypedContexts/PolicyContext.cs" and not "GeneratedPolicies/PolicyBodies_844D6619645B0916.cs" and not "GeneratedPolicies/PolicyBodies_C13C6DD636115D6A.cs").ToArray();
        Assert.Equal(15, unchanged.Length);
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
        var files = _hashedArtifacts.Where(artifact => artifact.RelativePath == "TypedContexts/PolicyContext.cs" || artifact.RelativePath == "GeneratedPolicies/PolicyBodies.cs" || (artifact.RelativePath.StartsWith("TypedContexts/TypedContext_", StringComparison.Ordinal) && artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))).ToArray();
        Assert.NotEmpty(files);
        Assert.All(files, artifact => Assert.All(RawText(artifact.RelativePath).Split('\n').Where(line => !line.TrimStart().StartsWith("using ", StringComparison.Ordinal)), line => Assert.DoesNotContain("global::", line, StringComparison.Ordinal)));
    }

    IEnumerable<PlannedArtifact> HashedArtifacts() => _hashedArtifacts.Where(artifact => !string.Equals(artifact.RelativePath, ".stage/completeness.json", StringComparison.Ordinal) && !string.Equals(artifact.RelativePath, "UNSUPPORTED.md", StringComparison.Ordinal));

    void AssertReducerBinding(Assembly assembly)
    {
        var fold = assembly.GetType("Invoices.Orders.Ordering.Totals.Fold", throwOnError: true);
        var transition = Assert.Single(fold.GetMethods(BindingFlags.NonPublic | BindingFlags.Static), method => method.Name.EndsWith("Transition", StringComparison.Ordinal));
        var wrapper = Assert.Single(transition.GetParameters()).ParameterType;
        Assert.Equal("Invoices.TypedContexts", wrapper.Namespace);
        Assert.Contains(_rawPlan.Artifacts, artifact => artifact.RelativePath == $"TypedContexts/{wrapper.Name}.cs");
        var text = EmittedText("Orders/Ordering/Totals/Fold.cs");
        Assert.Contains("using Invoices.TypedContexts;", text, StringComparison.Ordinal);
        Assert.Contains($"new {wrapper.Name}(", text, StringComparison.Ordinal);
        Assert.Contains($"{wrapper.Name} context)", text, StringComparison.Ordinal);
        Assert.DoesNotContain($"global::Invoices.TypedContexts.{wrapper.Name}", text, StringComparison.Ordinal);
        var observed = assembly.GetType("Invoices.Orders.Ordering.PlaceOrder.OrderPlaced", throwOnError: true);
        Assert.Equal(observed, wrapper.GetProperty("Event").PropertyType);
    }

    [Fact]
    public void should_restore_the_old_policy_runtime_bytes_by_restoring_only_the_type_comparisons()
    {
        const string path = "GeneratedPolicies/Policies.cs";
        var qualified = RawText(path)
            .Replace("using Cratis.Chronicle.Events;\n", string.Empty, StringComparison.Ordinal)
            .Replace("using Cratis.Concepts;\n", string.Empty, StringComparison.Ordinal)
            .Replace("typeof(ConceptAs<>)", "typeof(global::Cratis.Concepts.ConceptAs<>)", StringComparison.Ordinal)
            .Replace("typeof(EventSourceId<>)", "typeof(global::Cratis.Chronicle.Events.EventSourceId<>)", StringComparison.Ordinal);
        var restored = RemoveCallerIdentifier(QualifiedCommonOutput.BeforeTypeComparison(path, qualified));
        Assert.Equal(_oldRawHashes[path], Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(restored))));
    }

    // The new emitted output, spelled with the readable names; RawText reads the previous hashed spelling the old pins are about.
    string EmittedText(string path) => Encoding.UTF8.GetString(_rawPlan.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());

    string RawText(string path) => Encoding.UTF8.GetString(_hashedArtifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());

    // Re-frozen for #424: generated C# uses short type names and using directives; again for Cratis/Stage#848: authored bodies are indented to their block.
    // Re-frozen for Cratis/Stage#850: Policies.cs compares concept bases with typeof.
    // Re-frozen for Cratis/Stage#1061: the shared policy runtime reads the caller's identifier from the principal.
    // Cratis/Stage#850: the restored output reproduces the previous pin; the emitted output differs only by the readable helper names.
    [Fact] void should_restore_every_hashed_name_to_the_previous_pin() => _restoredDigest.ShouldEqual("CB5113C6BF8B5FC88786D81E5E5DCAE34817C30515FF025CDD5BADD9871677B2");
    [Fact] void should_keep_all_paths_and_bytes() => _digest.ShouldEqual("E1934997F24489C5DB546858250841205964CA995DAEBA4E56D27EED4B948935");
    [Fact] void should_confine_changes_to_common_and_using_placement() => _qualifiedDigest.ShouldEqual("19DDADC0074C9A4DB21FFCCC5B867876D9ADC779326625A25C05A704FB845BBC");
}
#endif
