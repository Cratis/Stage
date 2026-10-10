// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text;
using Cratis.Arc.Authorization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_combining_domain_policy_plans;

public class with_identical_operations : given.a_domain_policy_plan
{
    CratisPlanResult _north = null!;
    CratisPlanResult _south = null!;
    RenderedFile[] _files = null!;
    IReadOnlyList<string> _errors = null!;
    IReadOnlyList<string> _warnings = null!;

    void Establish() => _north = Plan("sales/retail");

    void Because()
    {
        _south = Plan("Support");
        var scaffold = CratisRendering.PlanScaffold(_options);
        _files = [.. _north.Artifacts.Concat(_south.Artifacts).Concat(scaffold.Artifacts)
            .GroupBy(artifact => artifact.RelativePath, StringComparer.Ordinal).Select(group => group.First())
            .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
        _errors = RenderedOutput.Errors(_files);
        _warnings = RenderedOutput.Warnings(_files);
    }

    [Fact] void should_admit_both_plans() => _north.Diagnostics.Concat(_south.Diagnostics).Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_not_overwrite_any_domain_file_in_either_order() => _north.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("Sales/Retail/", StringComparison.Ordinal)).Select(artifact => artifact.RelativePath).Intersect(_south.Artifacts.Select(artifact => artifact.RelativePath), StringComparer.Ordinal).ShouldBeEmpty();
    [Fact] void should_share_only_selection_independent_root_files() => _north.Artifacts.Join(_south.Artifacts, artifact => artifact.RelativePath, artifact => artifact.RelativePath, (north, south) => north.RelativePath).ShouldContainOnly(["GeneratedPolicies/Policies.cs", "GeneratedPolicies/PolicyBodies.cs", "TypedContexts/PolicyContext.cs", "TypedContexts/TenantId.cs", "GeneratedTenancy/PortableTenantValues.cs"]);
    [Fact] void should_keep_shared_files_byte_identical() => _north.Artifacts.Join(_south.Artifacts, artifact => artifact.RelativePath, artifact => artifact.RelativePath, (north, south) => north.Bytes.SequenceEqual(south.Bytes)).All(equal => equal).ShouldBeTrue();
    [Fact] void should_emit_three_distinct_operation_policy_paths_per_domain() => _north.Artifacts.Concat(_south.Artifacts).Where(artifact => Path.GetFileName(artifact.RelativePath).StartsWith("StagePolicy_", StringComparison.Ordinal)).Select(artifact => artifact.RelativePath).Distinct(StringComparer.Ordinal).Count().ShouldEqual(6);
    [Fact] void should_emit_distinct_opaque_body_paths_per_domain() => _north.Artifacts.Concat(_south.Artifacts).Where(artifact => Path.GetFileName(artifact.RelativePath).StartsWith("PolicyBodies_", StringComparison.Ordinal)).Select(artifact => artifact.RelativePath).Distinct(StringComparer.Ordinal).Count().ShouldEqual(4);
    [Fact] void should_emit_distinct_policy_and_reducer_wrapper_paths_per_domain() => _north.Artifacts.Concat(_south.Artifacts).Where(artifact => Path.GetFileName(artifact.RelativePath).StartsWith("TypedContext_", StringComparison.Ordinal)).Select(artifact => artifact.RelativePath).Distinct(StringComparer.Ordinal).Count().ShouldEqual(6);
    [Fact] void should_declare_distinct_domain_types() => DomainTypes("Sales/Retail/").Intersect(DomainTypes("Support/"), StringComparer.Ordinal).ShouldBeEmpty();
    [Fact] void should_compile_the_union_with_the_root_scaffold() => _errors.ShouldBeEmpty();
    [Fact] void should_compile_without_warnings() => _warnings.ShouldBeEmpty();

    [Fact]
    void should_register_distinct_names_matching_command_and_query_attributes()
    {
        var assembly = RenderedOutput.Load(_files);
        var services = new ServiceCollection();
        assembly.GetType("Invoices.GeneratedPolicies.Registration")!.GetMethod("Register")!.Invoke(null, [services]);
        var registrations = services.Select(service => service.ImplementationInstance).OfType<AuthorizationPolicyRegistration>().ToArray();
        registrations.Length.ShouldEqual(6);
        registrations.Select(registration => registration.Name).Distinct(StringComparer.Ordinal).Count().ShouldEqual(6);
        var attributes = assembly.GetTypes().SelectMany(type => type.GetCustomAttributes<AuthorizeAttribute>()
            .Select(attribute => (Target: type, attribute.Policy))
            .Concat(type.GetMethods().SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => (Target: type, attribute.Policy))))).ToArray();
        attributes.Length.ShouldEqual(6);
        attributes.All(attribute => registrations.Any(registration => registration.Name == attribute.Policy && registration.PolicyType.Namespace == attribute.Target.Namespace!.Split(".Billing", StringSplitOptions.None)[0].Split(".Orders", StringSplitOptions.None)[0] + ".GeneratedPolicies")).ShouldBeTrue();
    }

    string[] DomainTypes(string path)
    {
        var compilation = RenderedOutput.CreateCompilation(_files.Where(file => file.RelativePath.StartsWith(path, StringComparison.Ordinal)));
        return [.. compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Select(declaration => compilation.GetSemanticModel(tree).GetDeclaredSymbol(declaration)!.ToDisplayString())).Distinct(StringComparer.Ordinal)];
    }
}
#endif
