// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle.ReadModels;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer;

public class when_executing_performed_queries
{
    [Theory]
    [InlineData("live", "observable")]
    [InlineData("missing", "no resolved body")]
    [InlineData("changed", "SHA-256")]
    [InlineData("hash", "SHA-256")]
    [InlineData("compile", "CS0103")]
    [InlineData("authorized", "CS0103")]
    public async Task should_render_an_unverified_performer_as_a_compilable_gap(string failure, string reason)
    {
        var source = Source(failure is "compile" or "authorized" ? "return missing;" : "return readModels;", live: failure == "live");
        if (failure == "authorized") source = "policy Buyers\n  require claim \"role\" matches \"buyer\"\n" + source.Replace("module People", "module People\n  authorize Buyers", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var request = GapRequest((loaded, failure));
        var requirement = Assert.Single(request.ImplementationRequirements);
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.NotEmpty(plan.Artifacts);
        var diagnostic = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-IMPL-006"));
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(requirement.Source.SemanticId, diagnostic.Artifact);
        Assert.Contains(reason, diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        var gap = Assert.Single(plan.ImplementationGaps);
        Assert.Equal(ArtifactImplementationGapOrigin.Unverified, gap.Origin);
        Assert.Equal(requirement.RequirementId, gap.RequirementId);
        Assert.Equal(ArtifactRenderOutcome.Partial, plan.Outcome);
        Assert.True(plan.Success);
        var marker = Assert.Single(plan.Artifacts.Where(artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md"));
        Assert.Contains(requirement.RequirementId, System.Text.Encoding.UTF8.GetString(marker.Bytes.AsSpan()), StringComparison.Ordinal);
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        var rendered = SemanticStateViewArtifactRenderer.Render(context.SelectedSlices().Single(), context).Content;
        Assert.Contains($"Query 'FindPeople' (requirement {requirement.RequirementId}) is not runnable:", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("StagePerform_", rendered, StringComparison.Ordinal);
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        Assert.Empty(RenderedOutput.Errors(files));
        Assert.Empty(RenderedOutput.Warnings(files));
        var stub = Assert.Single(gap.Stubs);
        var baseline = System.Text.Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == stub.RelativePath).Bytes.AsSpan());
        Assert.Contains(stub.Signature, baseline, StringComparison.Ordinal);
        Assert.Contains("NotImplementedException", baseline.Substring(stub.BodyLocator.Start, stub.BodyLocator.Length), StringComparison.Ordinal);
        var assembly = RenderedOutput.Load(files);
        var method = assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)).Single(method => method.Name == "FindPeople");
        var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [.. Dependencies(Substitute.For<IReadModels>()), "Ada"]));
        var notImplemented = Assert.IsType<NotImplementedException>(exception.InnerException);
        Assert.Contains(requirement.RequirementId, notImplemented.Message, StringComparison.Ordinal);
        Assert.Contains(reason, notImplemented.Message, StringComparison.Ordinal);
    }

    static ArtifactRenderRequest GapRequest((LoadedSemanticModel Loaded, string Failure) scenario)
    {
        var request = Request(scenario.Loaded);
        var requirement = Assert.Single(request.ImplementationRequirements);
        return scenario.Failure switch
        {
            "missing" => request with { ImplementationContents = request.ImplementationContents.Remove(requirement.RequirementId) },
            "changed" => request with { ImplementationContents = request.ImplementationContents.SetItem(requirement.RequirementId, "return missing;") },
            "hash" => request with { ImplementationRequirements = [requirement with { ContentHash = "invalid" }] },
            _ => request
        };
    }

    const string Body = "return readModels.Where(row => row.Name == context.Arguments.Name && context.ReceivedAt.Year == 2026 && context.Identity.Identity?.IsAuthenticated == true && context.Tenant == \"tenant-a\");";

    [Fact]
    public async Task should_execute_a_snapshot_body_with_arguments_and_the_receipt_clock()
    {
        var loaded = await Load(Body);
        var plan = new CratisArtifactRenderPlanner().Plan(Request(loaded));
        Assert.True(plan.Success);
        Assert.Equal(ArtifactRenderOutcome.Complete, plan.Outcome);
        Assert.Empty(plan.ImplementationGaps);
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md");
        var method = Method(loaded);
        var (rows, row) = Rows(method);
        var readModels = Substitute.For<IReadModels>();
        SupplyMethod(nameof(Supply), method).Invoke(null, [readModels, rows]);
        var task = (Task)method.Invoke(null, [.. Dependencies(readModels), "Ada"])!;
        await task;
        var result = (System.Collections.IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.Same(row, Assert.Single(result.Cast<object>()));
    }

    [Fact]
    public async Task should_throw_instead_of_presenting_a_live_performer_as_a_body_owned_stream()
    {
        var loaded = await Load("return readModels;", live: true);
        var method = Method(loaded);
        Assert.Equal(typeof(System.Reactive.Subjects.ISubject<>), method.ReturnType.GetGenericTypeDefinition());
        var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [.. Dependencies(Substitute.For<IReadModels>()), "Ada"]));
        var gap = Assert.IsType<NotImplementedException>(exception.InnerException);
        Assert.Contains("no body-owned stream", gap.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task should_execute_a_single_result_with_a_by_argument()
    {
        var loaded = await Load("return readModels.SingleOrDefault(row => row.Name == context.Arguments.Name && row.Name == name);", single: true);
        var method = Method(loaded);
        var (rows, row) = Rows(method);
        var readModels = Substitute.For<IReadModels>();
        SupplyMethod(nameof(Supply), method).Invoke(null, [readModels, rows]);
        var task = (Task)method.Invoke(null, [.. Dependencies(readModels), "Ada"])!;
        await task;
        Assert.Same(row, task.GetType().GetProperty("Result")!.GetValue(task));
    }

    [Fact]
    public async Task should_keep_optional_filter_defaults_and_execute_them()
    {
        var loaded = await Load("return readModels.Where(row => name is null || row.Name == context.Arguments.Name);", optional: true);
        var method = Method(loaded);
        Assert.True(method.GetParameters()[^1].HasDefaultValue);
        Assert.Null(method.GetParameters()[^1].DefaultValue);
        var (rows, row) = Rows(method);
        var readModels = Substitute.For<IReadModels>();
        SupplyMethod(nameof(Supply), method).Invoke(null, [readModels, rows]);
        var task = (Task)method.Invoke(null, [.. Dependencies(readModels), null])!;
        await task;
        var result = (System.Collections.IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.Same(row, Assert.Single(result.Cast<object>()));
    }

    [Fact]
    public async Task should_keep_authorization_metadata_on_the_executable_query()
    {
        var source = "policy Buyers\n  require claim \"role\" matches \"buyer\"\n" + Source("return readModels;").Replace("module People", "module People\n  authorize Buyers", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        Assert.Contains(Method(loaded).GetCustomAttributesData(), attribute => attribute.AttributeType.Name == "AuthorizeAttribute");
    }

    [Theory]
    [InlineData("return missing;", "CS0103")]
    [InlineData("return 42;", "CS0029")]
    [InlineData("return readModels.Where(row => context.Occurred.Year == 2026);", "CS1061")]
    [InlineData("return readModels.Where(row => context.CausedBy != null);", "CS1061")]
    public async Task should_report_invalid_body_gaps_at_the_requirement(string body, string compilerCode)
    {
        var loaded = await Load(body);
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        var diagnostic = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-IMPL-006"));
        Assert.Equal(requirement.Source.SemanticId, diagnostic.Artifact);
        Assert.Contains(requirement.RequirementId, diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains(compilerCode, diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains(requirement.Source.Span.Document.ToString(), diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains($":{requirement.BodyLines[0].Line}:", diagnostic.Message, StringComparison.Ordinal);
        Assert.NotEmpty(plan.Artifacts);
    }

    [Fact]
    public async Task should_embed_a_file_body_with_the_same_behavior_as_inline_source()
    {
        var inline = await Load("return readModels;");
        var file = await LoadFile("return readModels;");
        var method = Method(file);
        Assert.Equal(Method(inline).ReturnType.ToString(), method.ReturnType.ToString());
        var (rows, row) = Rows(method);
        var readModels = Substitute.For<IReadModels>();
        SupplyMethod(nameof(Supply), method).Invoke(null, [readModels, rows]);
        var task = (Task)method.Invoke(null, [.. Dependencies(readModels), "Ada"])!;
        await task;
        var result = (System.Collections.IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.Same(row, Assert.Single(result.Cast<object>()));
    }

    [Fact]
    public async Task should_map_file_errors_to_the_attached_source()
    {
        var loaded = await LoadFile("return missing;");
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        var diagnostic = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-IMPL-006"));
        Assert.Contains("Queries/FindPeople.cs:1:", diagnostic.Message, StringComparison.Ordinal);
        Assert.NotEmpty(plan.Artifacts);
    }

    [Fact]
    public async Task should_refuse_changed_attachments_instead_of_rendering_them()
    {
        var loaded = await Load("return readModels;");
        var request = Request(loaded) with
        {
            ImplementationContents = loaded.ImplementationContents.SetItem(loaded.ImplementationRequirements.Single().RequirementId, "return missing;")
        };
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006");
        Assert.NotEmpty(plan.Artifacts);
    }

    [Fact]
    public void should_not_silently_drop_an_explicit_scope()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var source = Source("return readModels;").Replace("filter name String", "scoped to identity\n        filter name String", StringComparison.Ordinal);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("people"), "people", "People.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.False(compilation.Success);
        Assert.Contains(compilation.Diagnostics, diagnostic => diagnostic.Code == "PLAY0268");
    }

    [Fact]
    public async Task should_supply_an_anonymous_identity_when_both_principal_sources_are_absent()
    {
        var loaded = await Load("return readModels.Where(row => row.Name == name && !context.Identity.Identities.Any());");
        var method = Method(loaded);
        var (rows, row) = Rows(method);
        var readModels = Substitute.For<ObservableReadModels>();
        var dependencies = Dependencies(readModels);
        ((IQueryContextManager)dependencies[1]).Current.Returns(QueryContext.NotSet);
        ((ICurrentPrincipalAccessor)dependencies[3]).Current.Returns((ClaimsPrincipal?)null);
        SupplyMethod(nameof(Supply), method).Invoke(null, [readModels, rows]);
        var task = (Task)method.Invoke(null, [.. dependencies, "Ada"])!;
        await task;
        var snapshot = (System.Collections.IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.Same(row, Assert.Single(snapshot.Cast<object>()));
    }

    [Fact]
    public async Task should_prefer_the_authorized_principal_over_the_ambient_principal()
    {
        var loaded = await Load("return readModels.Where(row => row.Name == context.Identity.Identity!.Name);");
        var method = Method(loaded);
        var (rows, row) = Rows(method);
        var readModels = Substitute.For<IReadModels>();
        SupplyMethod(nameof(Supply), method).Invoke(null, [readModels, rows]);
        var dependencies = Dependencies(readModels);
        var authorizedContext = QueryContext.NotSet with { ReceivedAt = DateTimeOffset.MinValue };
        typeof(QueryContext).GetProperty(nameof(QueryContext.AuthorizedPrincipal))!.SetValue(authorizedContext,
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Ada")], "authorized")));
        ((IQueryContextManager)dependencies[1]).Current.Returns(authorizedContext);
        ((ICurrentPrincipalAccessor)dependencies[3]).Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Other")], "ambient")));
        var task = (Task)method.Invoke(null, [.. dependencies, "Ada"])!;
        await task;
        var result = (System.Collections.IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.Same(row, Assert.Single(result.Cast<object>()));
    }

    [Theory]
    [InlineData("java", false)]
    [InlineData("sql", false)]
    [InlineData("typescript", false)]
    [InlineData("java", true)]
    public async Task should_refuse_a_non_csharp_inline_performer_on_its_query(string language, bool live)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source("return readModels;", live: live).Replace("```csharp", $"```{language}", StringComparison.Ordinal));
        AssertLanguageRefusal(Request(loaded));
    }

    [Theory]
    [InlineData("java")]
    [InlineData("ts")]
    public async Task should_refuse_a_non_csharp_file_performer_on_its_query(string extension)
    {
        var loaded = await LoadFile("return readModels;", extension);
        AssertLanguageRefusal(Request(loaded));
    }

    static void AssertLanguageRefusal(ArtifactRenderRequest request)
    {
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        var requirement = Assert.Single(request.ImplementationRequirements);
        var diagnostic = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-019"));
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(requirement.Source.SemanticId, diagnostic.Artifact);
        Assert.Contains("language", diagnostic.Message, StringComparison.Ordinal);
        Assert.Empty(plan.Artifacts);
        Assert.Empty(plan.ImplementationGaps);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006");
    }

    [Fact]
    public async Task should_admit_and_execute_the_rendered_scalar_and_identifier_concept_sentinels()
    {
        // The read model key makes PersonId an identifier concept; PersonName stays a scalar concept.
        const string body = "return readModels.Where(row => row.PersonId != PersonId.NotSet && row.Name != PersonName.NotSet && row.PersonId.TypedValue != default(Guid));";
        var source = "concept PersonId : Uuid\nconcept PersonName : String\n" + Source(body)
            .Replace("personId Uuid", "personId PersonId", StringComparison.Ordinal)
            .Replace("        name String", "        name PersonName", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var request = Request(loaded);
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        Assert.Contains(loaded.Model.Application.Concepts.Single(concept => concept.Name == "PersonId").Id, context.IdentifierConcepts);
        var method = Method(loaded);
        var constructor = method.DeclaringType!.GetConstructors().Single();
        var properties = constructor.GetParameters();
        var row = constructor.Invoke([
            Activator.CreateInstance(properties[0].ParameterType, Guid.NewGuid()),
            Activator.CreateInstance(properties[1].ParameterType, "Ada")
        ]);
        var rows = Array.CreateInstance(method.DeclaringType, 1);
        rows.SetValue(row, 0);
        var readModels = Substitute.For<IReadModels>();
        SupplyMethod(nameof(Supply), method).Invoke(null, [readModels, rows]);
        var task = (Task)method.Invoke(null, [.. Dependencies(readModels), "Ada"])!;
        await task;
        var result = (System.Collections.IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.Same(row, Assert.Single(result.Cast<object>()));
    }

    [Theory]
    [InlineData("readModelStore")]
    [InlineData("queryContexts")]
    [InlineData("tenantAccessor")]
    [InlineData("principalAccessor")]
    [InlineData("source")]
    public async Task should_not_collide_with_adapter_local_names(string name)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source($"return readModels.Where(row => row.Name == {name});", live: true)
            .Replace("filter name String", $"filter {name} String", StringComparison.Ordinal));
        Assert.NotNull(Method(loaded));
    }

    [Theory]
    [InlineData("context")]
    [InlineData("readModels")]
    public async Task should_diagnose_arguments_that_collide_with_body_bindings(string name)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source("return readModels;").Replace("filter name String", $"filter {name} String", StringComparison.Ordinal));
        var plan = new CratisArtifactRenderPlanner().Plan(Request(loaded));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006" && diagnostic.Message.Contains("reserved query performer body binding", StringComparison.Ordinal));
        if (string.Equals(name, "readModels", StringComparison.Ordinal))
        {
            Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012");
            Assert.Empty(plan.Artifacts);
        }
        else
        {
            Assert.NotEmpty(plan.Artifacts);
        }
    }

    static Task<LoadedSemanticModel> Load(string body, bool live = false, bool single = false, bool optional = false) =>
        when_rendering_a_pure_reducer.Load(Source(body, live, single, optional));

    static string Source(string body, bool live = false, bool single = false, bool optional = false) => $"""
        module People
          feature Directory
            slice StateView People
              readmodel PersonSummary
                personId Uuid
                name String
              query FindPeople => {(live ? "observable " : string.Empty)}PersonSummary{(single ? " optional" : "[]")}
                {(single ? "by" : "filter")} name String{(optional ? " optional" : string.Empty)}
                performer
                  ```csharp
                  {body}
                  ```
        """;

    static async Task<LoadedSemanticModel> LoadFile(string body, string extension = "cs")
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, ".git")) && !Directory.Exists(Path.Combine(root.FullName, ".git"))) root = root.Parent;
        Assert.NotNull(root);
        var folder = Path.Combine(root.FullName, ".ai-work", "stage-performer", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "Queries"));
        try
        {
            var source = Source("return readModels;").Replace("```csharp\n          return readModels;\n          ```", $"file Queries/FindPeople.{extension}", StringComparison.Ordinal);
            await File.WriteAllTextAsync(Path.Combine(folder, "People.play"), source);
            await File.WriteAllTextAsync(Path.Combine(folder, "Queries", $"FindPeople.{extension}"), body);
            return await SemanticModelLoader.LoadFromPathAsync(folder, null, "Projects");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    static ArtifactRenderRequest Request(LoadedSemanticModel loaded) =>
        new(loaded.Model, SemanticExecutionPlan.Compile(loaded.Model).Plan!, CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };

    static MethodInfo Method(LoadedSemanticModel loaded)
    {
        var plan = new CratisArtifactRenderPlanner().Plan(Request(loaded));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        Assert.Empty(RenderedOutput.Errors(files));
        Assert.Empty(RenderedOutput.Warnings(files));
        var assembly = RenderedOutput.Load(files);
        return assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)).Single(method => method.Name == "FindPeople");
    }

    static System.Reactive.Subjects.Subject<IEnumerable<T>> SupplyLive<T>(IReadModels readModels)
        where T : class
    {
        var source = new System.Reactive.Subjects.Subject<IEnumerable<T>>();
        readModels.Observe<T>().Returns(source);
        return source;
    }

    static void AssertLive<T>(System.Reactive.Subjects.Subject<IEnumerable<T>> source, System.Reactive.Subjects.ISubject<IEnumerable<T>> result, Array rows)
    {
        var emissions = new List<T[]>();
        using (result.Subscribe(value => emissions.Add([.. value])))
        {
            source.OnNext(rows.Cast<T>());
            source.OnNext([]);
            source.OnNext(rows.Cast<T>());
        }

        source.OnNext(rows.Cast<T>());
        Assert.Equal(3, emissions.Count);
        Assert.Equal(rows.GetValue(0), Assert.Single(emissions[0]));
        Assert.Empty(emissions[1]);
        Assert.Equal(rows.GetValue(0), Assert.Single(emissions[2]));
        source.Dispose();
    }

    static object[] Dependencies(IReadModels readModels)
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(2026, 10, 20, 23, 59, 0, TimeSpan.Zero));
        var receivedAt = clock.GetUtcNow();
        var manager = Substitute.For<IQueryContextManager>();
        manager.Current.Returns(QueryContext.NotSet with { ReceivedAt = receivedAt });
        clock.GetUtcNow().Returns(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var tenant = Substitute.For<ITenantIdAccessor>();
        tenant.Current.Returns(new TenantId("tenant-a"));
        var caller = Substitute.For<ICurrentPrincipalAccessor>();
        caller.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([], "test")));
        return [readModels, manager, tenant, caller];
    }

    static (Array Rows, object Row) Rows(MethodInfo method)
    {
        var model = method.DeclaringType!;
        var constructor = model.GetConstructors().Single(candidate => candidate.GetParameters().Length == 2);
        var values = constructor.GetParameters().Select(parameter => parameter.ParameterType == typeof(Guid) ? (object)Guid.NewGuid() : "Ada").ToArray();
        var row = constructor.Invoke(values);
        var rows = Array.CreateInstance(model, 1);
        rows.SetValue(row, 0);
        return (rows, row);
    }

    static MethodInfo SupplyMethod(string name, MethodInfo query) =>
        typeof(when_executing_performed_queries).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(query.DeclaringType!);

    static void Supply<T>(IReadModels readModels, Array rows)
        where T : class => readModels.GetInstances<T>().Returns(Task.FromResult(rows.Cast<T>().AsEnumerable()));
}
#endif
