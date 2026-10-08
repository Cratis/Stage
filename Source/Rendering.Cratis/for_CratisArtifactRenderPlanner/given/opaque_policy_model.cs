// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// An invoice model whose command and query use an inline csharp policy, rendered with the attachment envelope and
/// typed contexts of one compilation.
/// </summary>
public static class opaque_policy_model
{
    public static readonly DateTimeOffset Receipt = new(2027, 3, 4, 10, 0, 0, TimeSpan.Zero);

    public static string Source(string body, string commandAuthorization = "Custom", string queryAuthorization = "Custom")
    {
        var indented = string.Join('\n', body.Split('\n').Select(line => $"  {line}"));
        return $$"""
            concept InvoiceId : String
            policy Custom
              ```csharp
            {{indented}}
              ```
            policy Staff
              require role "Staff"
            policy Guests
              require not authenticated
            module Billing
              feature Invoicing
                slice StateChange Issue
                  command IssueInvoice
                    authorize {{commandAuthorization}}
                    invoiceId InvoiceId identifier
                    description String
                    produces InvoiceIssued
                      for invoiceId
                      invoiceId = invoiceId
                      description = description
                  event InvoiceIssued
                    invoiceId InvoiceId
                    description String
                slice StateView Lookup
                  readmodel InvoiceSummary
                    invoiceId InvoiceId
                    description String
                  query InvoiceById => InvoiceSummary?
                    by invoiceId InvoiceId
                    authorize {{queryAuthorization}}
                  projection InvoiceSummaryProjection => InvoiceSummary
                    from InvoiceIssued key invoiceId
                      invoiceId = invoiceId
                      description = description
            """;
    }

    public static LoadedSemanticModel Load(string source)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, ".git")) && !Directory.Exists(Path.Combine(root.FullName, ".git")))
        {
            root = root.Parent;
        }

        var folder = Path.Combine(root!.FullName, ".ai-work", "stage-namespace", "policy-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "Invoices.play"), source);
            return SemanticModelLoader.LoadFromPathAsync(folder, null, "InvoiceModel").GetAwaiter().GetResult();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    public static ArtifactRenderPlan Plan(LoadedSemanticModel loaded) =>
        CratisRendering.Plan(
            loaded.Model,
            loaded.Plan,
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id),
            new("InvoiceApp", "Invoices"),
            loaded.ImplementationRequirements,
            loaded.ImplementationContents,
            loaded.AttachmentDiagnostics,
            loaded.TypedContextDescriptors);

    public static ArtifactRenderPlan Plan(string source) => Plan(Load(source));

    public static Assembly Compile(ArtifactRenderPlan plan) =>
        RenderedOutput.Load(Files(plan));

    public static IEnumerable<RenderedFile> Files(ArtifactRenderPlan plan) =>
        plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));

    public static string Errors(ArtifactRenderPlan plan) =>
        string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}"));

    public static ClaimsPrincipal Caller(params string[] roles) =>
        new(new ClaimsIdentity(roles.Select(role => new Claim(ClaimTypes.Role, role)), "fixture"));

    public static ClaimsPrincipal Guest() => new(new ClaimsIdentity());

    public static bool AllowsCommand(Assembly assembly, ExecutableSemanticModel model, ClaimsPrincipal principal, DateTimeOffset receivedAt, string invoice = "invoice-one")
    {
        var command = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single();
        var type = assembly.GetTypes().Single(candidate => candidate.Name == "IssueInvoice");
        var id = Activator.CreateInstance(assembly.GetTypes().Single(candidate => candidate.Name == "InvoiceId"), invoice)!;
        var instance = Activator.CreateInstance(type, id, "North")!;
        return Allows(assembly, command.Id, type, new CommandContext(CorrelationId.New(), type, instance, [], CommandContextValues.Empty), principal, receivedAt);
    }

    public static bool AllowsQuery(Assembly assembly, ExecutableSemanticModel model, ClaimsPrincipal principal, DateTimeOffset receivedAt, object? invoice)
    {
        var query = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Queries).Single();
        var target = assembly.GetTypes().Single(candidate => candidate.Name == "InvoiceSummary").GetMethod("InvoiceById")!;
        var arguments = new QueryArguments();
        if (invoice is not null)
        {
            arguments["invoiceId"] = invoice;
        }

        return Allows(assembly, query.Id, target, new QueryContext("InvoiceById", CorrelationId.New(), Paging.NotPaged, Sorting.None, arguments), principal, receivedAt);
    }

    public static bool EvaluatesAnonymous(Assembly assembly, SemanticId operation)
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        assembly.GetTypes().Single(type => type.FullName == "Invoices.GeneratedPolicies.Registration").GetMethod("Register")!.Invoke(null, [services]);
        return services.Select(service => service.ImplementationInstance).OfType<AuthorizationPolicyRegistration>()
            .Single(registration => registration.Name == SemanticPolicyArtifactRenderer.Name(operation)).EvaluatesAnonymous;
    }

    static bool Allows(Assembly assembly, SemanticId operation, MemberInfo target, object resource, ClaimsPrincipal principal, DateTimeOffset receivedAt)
    {
        var policy = (IAuthorizationPolicy)Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == SemanticPolicyArtifactRenderer.Name(operation)))!;
        return policy.IsAuthorized(new(principal, target, resource) { ReceivedAt = receivedAt }, CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }
}
#endif
