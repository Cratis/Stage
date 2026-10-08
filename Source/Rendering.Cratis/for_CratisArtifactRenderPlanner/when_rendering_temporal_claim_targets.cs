// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_temporal_claim_targets : Specification
{
    readonly Dictionary<string, (Assembly Assembly, ExecutableSemanticModel Model)> _applications = [];

    void Because()
    {
        foreach (var primitive in new[] { "Date", "DateTime" })
        {
            var source = when_rendering_portable_authorization.Source
                .Replace("concept InvoiceId : String", $"concept InvoiceId : {primitive}\nconcept Deadline : {primitive}", StringComparison.Ordinal)
                .Replace("  authorize Access\n", string.Empty, StringComparison.Ordinal)
                .Replace("claim \"owner\" matches subject and claim \"region\" matches description", "claim \"owner\" matches subject and claim \"owner\" matches invoiceId and not claim \"blocked\" matches deadline", StringComparison.Ordinal)
                .Replace("        description String\n        produces", "        description String\n        deadline Deadline optional\n        produces", StringComparison.Ordinal);
            var model = invoice_model.Compile(source);
            var plan = invoice_model.Plan(model);
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
            var sources = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
                .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
            _applications.Add(primitive, (RenderedOutput.Load(sources), model));
        }
    }

    [Theory]
    [InlineData("Date", "2026-09-07")]
    [InlineData("DateTime", "2026-09-07T12:34:56.1234567Z")]
    [InlineData("DateTime", "2026-09-07T12:34:56.1234567+02:00")]
    public void should_match_typed_concepts_primitives_and_canonical_query_text(string primitive, string canonical)
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var typed = Typed(primitive, canonical);
            AllowsQuery(primitive, typed, canonical).ShouldBeTrue();
            AllowsQuery(primitive, Concept(primitive, "InvoiceId", typed), canonical).ShouldBeTrue();
            AllowsQuery(primitive, canonical, canonical).ShouldBeTrue();
            AllowsCommand(primitive, canonical, canonical, canonical).ShouldBeTrue();
            Reference(primitive, canonical, canonical, canonical).ShouldBeTrue();
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData("Date", "2026-09-07", "07/09/2026")]
    [InlineData("Date", "2026-09-07", "2026-9-7")]
    [InlineData("Date", "2026-09-07", "")]
    [InlineData("DateTime", "2026-09-07T12:34:56.1234567Z", "2026-09-07T12:34:56.1234567+00:00")]
    [InlineData("DateTime", "2026-09-07T12:34:56.1234567Z", "2026-09-07T14:34:56.1234567+02:00")]
    [InlineData("DateTime", "2026-09-07T12:34:56.1234567Z", "2026-09-07T12:34:56Z")]
    [InlineData("DateTime", "2026-09-07T12:34:56.1234567Z", "")]
    public void should_compare_claims_ordinally_without_normalization(string primitive, string canonical, string claim)
    {
        AllowsQuery(primitive, Typed(primitive, canonical), claim).ShouldBeFalse();
        AllowsCommand(primitive, canonical, claim, canonical).ShouldBeFalse();
        Reference(primitive, canonical, claim, canonical).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Date", "07/09/2026")]
    [InlineData("Date", "2026-9-7")]
    [InlineData("Date", "2026-02-30")]
    [InlineData("Date", "")]
    [InlineData("DateTime", "2026-09-07T12:34:56.1234567+00:00")]
    [InlineData("DateTime", "2026-09-07T12:34:56Z")]
    [InlineData("DateTime", "07/09/2026 12:34:56")]
    [InlineData("DateTime", "")]
    public void should_deny_noncanonical_query_text_even_with_an_identical_claim(string primitive, string text) => AllowsQuery(primitive, text, text).ShouldBeFalse();

    [Theory]
    [InlineData("Date", "2026-09-07")]
    [InlineData("DateTime", "2026-09-07T12:34:56.1234567Z")]
    public void should_keep_a_negated_unknown_target_unknown(string primitive, string canonical)
    {
        AllowsCommand(primitive, canonical, canonical, null).ShouldBeFalse();
        Reference(primitive, canonical, canonical, null).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Date")]
    [InlineData("DateTime")]
    public void should_refuse_a_value_of_the_wrong_runtime_type(string primitive) => AllowsQuery(primitive, Guid.Empty, Guid.Empty.ToString("D")).ShouldBeFalse();

    bool AllowsQuery(string primitive, object argument, string claim)
    {
        var (assembly, model) = _applications[primitive];
        var query = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Queries).Single();
        var target = assembly.GetTypes().Single(type => type.Name == "InvoiceSummary").GetMethod("InvoiceById")!;
        var resource = new QueryContext("InvoiceById", CorrelationId.New(), Paging.NotPaged, Sorting.None, new QueryArguments { ["invoiceId"] = argument });
        return Allows(assembly, query.Id, target, resource, claim);
    }

    bool AllowsCommand(string primitive, string canonical, string claim, string? deadline)
    {
        var (assembly, model) = _applications[primitive];
        var command = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single();
        var type = assembly.GetTypes().Single(type => type.Name == "IssueInvoice");
        var instance = Activator.CreateInstance(type, Concept(primitive, "InvoiceId", Typed(primitive, canonical)), "North", deadline is null ? null : Concept(primitive, "Deadline", Typed(primitive, deadline)))!;
        return Allows(assembly, command.Id, type, new CommandContext(CorrelationId.New(), type, instance, [], CommandContextValues.Empty), claim);
    }

    bool Reference(string primitive, string canonical, string claim, string? deadline)
    {
        var model = _applications[primitive].Model;
        var command = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single();
        var values = command.Properties.Select(property => new SemanticPropertyValue(property.Id, property.Name switch
        {
            "invoiceId" => SemanticValue.Text(canonical),
            "description" => SemanticValue.Text("North"),
            _ => deadline is null ? SemanticValue.Null : SemanticValue.Text(deadline)
        })).ToImmutableArray();
        var request = SemanticExecutionRequest.Create(command.Id, values, []) with { Caller = new(true, [], [new("OWNER", claim)]) };
        return new SemanticEvaluator().Execute(SemanticExecutionPlan.Compile(model).Plan!, SemanticWorld.Empty, request) is not SemanticRejected { Category: SemanticRejectionCategory.Unauthorized };
    }

    object Concept(string primitive, string name, object value) => Activator.CreateInstance(_applications[primitive].Assembly.GetTypes().Single(type => type.Name == name), value)!;

    static object Typed(string primitive, string canonical) => primitive == "Date" ? DateOnly.Parse(canonical, CultureInfo.InvariantCulture) : DateTimeOffset.Parse(canonical, CultureInfo.InvariantCulture);

    static bool Allows(Assembly assembly, SemanticId operation, MemberInfo target, object resource, string claim)
    {
        var policy = (IAuthorizationPolicy)Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == SemanticPolicyArtifactRenderer.Name(operation)))!;
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("OWNER", claim)], "fixture"));
        return policy.IsAuthorized(new(principal, target, resource), CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }
}
