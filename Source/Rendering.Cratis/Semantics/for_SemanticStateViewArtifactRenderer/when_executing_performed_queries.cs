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
    const string Body = "return readModels.Where(row => row.Name == context.Arguments.Name && context.ReceivedAt.Year == 2026 && context.Identity.Identity?.IsAuthenticated == true && context.Tenant == \"tenant-a\");";

    [Fact]
    public async Task should_execute_a_snapshot_body_with_arguments_and_the_receipt_clock()
    {
        var loaded = await Load(Body);
        var method = Method(loaded);
        var (rows, row) = Rows(method);
        var readModels = Substitute.For<IReadModels>();
        SupplyMethod(nameof(Supply), method).Invoke(null, [readModels, rows]);
        var task = (Task)method.Invoke(null, Dependencies(readModels).Append("Ada").ToArray())!;
        await task;
        var result = (System.Collections.IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.Same(row, Assert.Single(result.Cast<object>()));
    }

    [Fact]
    public async Task should_apply_a_live_body_on_each_emission_with_the_original_receipt_context()
    {
        var loaded = await Load("return readModels.Where(row => row.Name == name && context.Arguments.Name == name && context.ReceivedAt.Year == 2026 && context.Tenant == \"tenant-a\");", live: true);
        var method = Method(loaded);
        Assert.Equal(typeof(System.Reactive.Subjects.ISubject<>), method.ReturnType.GetGenericTypeDefinition());
        var (rows, _) = Rows(method);
        var readModels = Substitute.For<IReadModels>();
        var source = SupplyMethod(nameof(SupplyLive), method).Invoke(null, [readModels])!;
        var dependencies = Dependencies(readModels);
        var result = method.Invoke(null, dependencies.Append("Ada").ToArray())!;
        ((IQueryContextManager)dependencies[1]).Current.Returns(QueryContext.NotSet with { ReceivedAt = DateTimeOffset.MinValue });
        ((ITenantIdAccessor)dependencies[2]).Current.Returns(new TenantId("other"));
        SupplyMethod(nameof(AssertLive), method).Invoke(null, [source, result, rows]);
    }

    [Fact]
    public async Task should_execute_a_single_result_with_a_by_argument()
    {
        var loaded = await Load("return readModels.SingleOrDefault(row => row.Name == context.Arguments.Name && row.Name == name);", single: true);
        var method = Method(loaded);
        var (rows, row) = Rows(method);
        var readModels = Substitute.For<IReadModels>();
        SupplyMethod(nameof(Supply), method).Invoke(null, [readModels, rows]);
        var task = (Task)method.Invoke(null, Dependencies(readModels).Append("Ada").ToArray())!;
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
        var task = (Task)method.Invoke(null, Dependencies(readModels).Append(null).ToArray())!;
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
    public async Task should_refuse_invalid_bodies_at_the_requirement(string body, string compilerCode)
    {
        var loaded = await Load(body);
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        var diagnostic = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-IMPL-005"));
        Assert.Equal(requirement.Source.SemanticId, diagnostic.Artifact);
        Assert.Contains(requirement.RequirementId, diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains(compilerCode, diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains(requirement.Source.Span.Document.ToString(), diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains($":{requirement.BodyLines[0].Line}:", diagnostic.Message, StringComparison.Ordinal);
        Assert.Empty(plan.Artifacts);
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
        var task = (Task)method.Invoke(null, Dependencies(readModels).Append("Ada").ToArray())!;
        await task;
        var result = (System.Collections.IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.Same(row, Assert.Single(result.Cast<object>()));
    }

    [Fact]
    public async Task should_map_file_errors_to_the_attached_source()
    {
        var loaded = await LoadFile("return missing;");
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        var diagnostic = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-IMPL-005"));
        Assert.Contains("Queries/FindPeople.cs:1:", diagnostic.Message, StringComparison.Ordinal);
        Assert.Empty(plan.Artifacts);
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
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-020");
        Assert.Empty(plan.Artifacts);
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
    public void should_not_silently_supply_a_context_sourced_by_argument()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var source = Source("return readModels;", single: true).Replace("by name String", "by name String from $context.identity", StringComparison.Ordinal);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("people"), "people", "People.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.False(compilation.Success);
        Assert.Contains(compilation.Diagnostics, diagnostic => diagnostic.Code == "PLAY0268");
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

    static async Task<LoadedSemanticModel> LoadFile(string body)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, ".git")) && !Directory.Exists(Path.Combine(root.FullName, ".git"))) root = root.Parent;
        Assert.NotNull(root);
        var folder = Path.Combine(root.FullName, ".ai-work", "stage-performer", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "Queries"));
        try
        {
            var source = Source("return readModels;").Replace("```csharp\n          return readModels;\n          ```", "file Queries/FindPeople.cs", StringComparison.Ordinal);
            await File.WriteAllTextAsync(Path.Combine(folder, "People.play"), source);
            await File.WriteAllTextAsync(Path.Combine(folder, "Queries", "FindPeople.cs"), body);
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
        var assembly = RenderedOutput.Load(files);
        return assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)).Single(method => method.Name == "FindPeople");
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
        using (result.Subscribe(value => emissions.Add(value.ToArray())))
        {
            source.OnNext(rows.Cast<T>());
            source.OnNext([]);
            source.OnNext(rows.Cast<T>());
        }

        source.OnNext(rows.Cast<T>());
        Assert.Equal(3, emissions.Count);
        Assert.Same(rows.GetValue(0), Assert.Single(emissions[0]));
        Assert.Empty(emissions[1]);
        Assert.Same(rows.GetValue(0), Assert.Single(emissions[2]));
        source.Dispose();
    }
}
#endif
