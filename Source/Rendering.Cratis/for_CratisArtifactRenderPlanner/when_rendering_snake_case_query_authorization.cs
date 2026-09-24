// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using System.Text;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_snake_case_query_authorization : Specification
{
    IAuthorizationPolicy _policy = null!;
    MethodInfo _method = null!;
    object _mine = null!;
    object _victim = null!;

    void Establish()
    {
        var source = when_rendering_portable_authorization.Source.Replace("invoiceId", "invoice_id", StringComparison.Ordinal);
        var model = invoice_model.Compile(source);
        var plan = invoice_model.Plan(model);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var sources = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var assembly = RenderedOutput.Load(sources);
        var query = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Queries.Length == 1).Queries.Single();
        _policy = (IAuthorizationPolicy)Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == SemanticPolicyArtifactRenderer.Name(query.Id)))!;
        _method = assembly.GetTypes().Single(type => type.Name == "InvoiceSummary").GetMethod("InvoiceById")!;
        var id = assembly.GetTypes().Single(type => type.Name == "InvoiceId");
        _mine = Activator.CreateInstance(id, "mine")!;
        _victim = Activator.CreateInstance(id, "victim")!;
    }

    [Fact] void should_bind_the_policy_to_the_actual_method_parameter() => _method.GetParameters()[^1].Name.ShouldEqual("invoiceId");
    [Fact] void should_deny_the_raw_model_key_when_it_disagrees_with_the_bound_argument() => Allows(new() { ["invoice_id"] = _mine, ["invoiceId"] = _victim }).ShouldBeFalse();
    [Fact] void should_allow_the_owner_of_the_bound_argument() => Allows(new() { ["invoiceId"] = _mine }).ShouldBeTrue();

    bool Allows(QueryArguments arguments)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("owner", "mine"), new Claim(ClaimTypes.Role, "Staff")], "fixture"));
        var resource = new QueryContext("InvoiceById", CorrelationId.New(), Paging.NotPaged, Sorting.None, arguments);
        return _policy.IsAuthorized(new(principal, _method, resource), CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }
}
