// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using System.Text;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_uuid_ownership_query_arguments : Specification
{
    const string CanonicalUuid = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    ArtifactRenderPlan _plan = null!;
    MethodInfo _query = null!;
    IAuthorizationPolicy _policy = null!;

    void Because()
    {
        var source = when_rendering_portable_authorization.Source
            .Replace("concept InvoiceId : String", "concept InvoiceId : Uuid", StringComparison.Ordinal)
            .Replace("  authorize Access\n", string.Empty, StringComparison.Ordinal);
        var model = invoice_model.Compile(source);
        _plan = invoice_model.Plan(model);
        if (!_plan.Success) return;
        var sources = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var assembly = RenderedOutput.Load(sources);
        var query = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Queries.Length == 1).Queries.Single();
        _query = assembly.GetTypes().Single(type => type.Name == "InvoiceSummary").GetMethod("InvoiceById")!;
        _policy = (IAuthorizationPolicy)Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == SemanticPolicyArtifactRenderer.Name(query.Id)))!;
    }

    [Fact] void should_plan_uuid_ownership() => _plan.Success.ShouldBeTrue();
    [Fact] void should_allow_a_typed_guid_with_a_canonical_claim() => Allows(new Guid(CanonicalUuid), CanonicalUuid).ShouldBeTrue();
    [Fact] void should_allow_a_canonical_string_with_a_canonical_claim() => Allows(CanonicalUuid, CanonicalUuid).ShouldBeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("3FA85F64-5717-4562-B3FC-2C963F66AFA6")]
    [InlineData("{3fa85f64-5717-4562-b3fc-2c963f66afa6}")]
    [InlineData("3fa85f6457174562b3fc2c963f66afa6")]
    [InlineData("not-a-uuid")]
    public void should_deny_noncanonical_string_arguments_even_with_an_identical_claim(string argument) => Allows(argument, argument).ShouldBeFalse();

    [Theory]
    [InlineData("")]
    [InlineData("3FA85F64-5717-4562-B3FC-2C963F66AFA6")]
    [InlineData("{3fa85f64-5717-4562-b3fc-2c963f66afa6}")]
    [InlineData("3fa85f6457174562b3fc2c963f66afa6")]
    public void should_deny_noncanonical_string_arguments_with_a_canonical_claim(string argument) => Allows(argument, CanonicalUuid).ShouldBeFalse();

    bool Allows(object argument, string claim)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("owner", claim)], "fixture"));
        var resource = new QueryContext("InvoiceById", CorrelationId.New(), Paging.NotPaged, Sorting.None, new QueryArguments { ["invoiceId"] = argument });

        return _policy.IsAuthorized(new(principal, _query, resource), CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }
}
