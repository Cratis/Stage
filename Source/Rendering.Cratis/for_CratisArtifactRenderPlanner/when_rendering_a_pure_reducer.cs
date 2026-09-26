// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_a_pure_reducer
{
    internal const string Source = """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                id Uuid identifier
                amount Decimal
                produces OrderPlaced
                  for id
                  id = id
                  amount = amount
              event OrderPlaced
                id Uuid
                amount Decimal
            slice StateView Totals
              readmodel Total
                id Uuid
                amount Decimal
              query ById => Total?
                by id Uuid
              reducer Fold => Total
                on OrderPlaced
                  ```csharp
                  return new Total(Guid.Parse("00000000-0000-0000-0000-000000000001"), context.Event.Amount);
                  ```
        """;

    [Fact]
    public async Task should_render_the_reducer_and_its_wrapped_body()
    {
        var compilation = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var plan = Plan(compilation);
        Assert.True(plan.Success, $"body={string.Join('|', compilation.ImplementationContents.Values)}\n{string.Join(Environment.NewLine, plan.Diagnostics)}");
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.StartsWith("TypedContexts/TypedContext_", StringComparison.Ordinal));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var errors = RenderedOutput.Errors(files);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    [Fact]
    public async Task should_generate_a_keyed_reducer_read_model_specification()
    {
        var source = WithIdentifierConcept(Source).Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id OrderId\n        amount Decimal", "      event OrderPlaced\n        id OrderId\n        amount Decimal\n      specification CreatingATotal\n        when PlaceOrder\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then readmodel Total\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20", StringComparison.Ordinal);
        Assert.Contains("specification CreatingATotal", source, StringComparison.Ordinal);
        var plan = Plan(await Load(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var generatedSpec = plan.Artifacts.Single(artifact => artifact.RelativePath.Contains("is_projected", StringComparison.Ordinal));
        Assert.Contains("[Fact]", System.Text.Encoding.UTF8.GetString(generatedSpec.Bytes.AsSpan()), StringComparison.Ordinal);
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var errors = RenderedOutput.Errors(files);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    [Fact]
    public async Task should_render_file_and_inline_bodies_identically()
    {
        var inline = Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal);
        var file = inline.Replace("```csharp\n          return new Total(Guid.Empty, context.Event.Amount);\n          ```", "file Reducers/Fold.cs", StringComparison.Ordinal);
        Assert.NotEqual(inline, file);
        var first = Plan(await Load(inline));
        var second = Plan(await Load(file));
        Assert.True(first.Success, string.Join(Environment.NewLine, first.Diagnostics));
        Assert.True(second.Success, string.Join(Environment.NewLine, second.Diagnostics));
        var firstContent = System.Text.Encoding.UTF8.GetString(first.Artifacts.Single(_ => _.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        var secondContent = System.Text.Encoding.UTF8.GetString(second.Artifacts.Single(_ => _.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        Assert.True(firstContent == secondContent, $"Inline:\n{firstContent}\nFile:\n{secondContent}");
    }

    [Fact]
    public async Task should_keep_the_generated_tenant_token_in_reflection_parity_with_screenplay()
    {
        var compiled = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var plan = Plan(compiled);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var token = plan.Artifacts.Single(_ => _.RelativePath == "TypedContexts/TenantId.cs");
        var assembly = RenderedOutput.Load([new RenderedFile(token.RelativePath, System.Text.Encoding.UTF8.GetString(token.Bytes.AsSpan()))]);
        var actual = assembly.GetType("Projects.TypedContexts.TenantId")!;
        var expected = typeof(global::Cratis.Screenplay.Contexts.TenantId);
        Assert.Equal(Members(expected), Members(actual));
        Assert.Equal(global::Cratis.Screenplay.Contexts.TenantId.Default.Value, actual.GetField("Default")!.GetValue(null)!.GetType().GetProperty("Value")!.GetValue(actual.GetField("Default")!.GetValue(null)));
        Assert.Equal(global::Cratis.Screenplay.Contexts.TenantId.NotSet.Value, actual.GetField("NotSet")!.GetValue(null)!.GetType().GetProperty("Value")!.GetValue(actual.GetField("NotSet")!.GetValue(null)));

        static string[] Members(Type type) => [.. type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(member => member switch
            {
                MethodInfo method => $"method/{member.Name}/{Normalize(method.ReturnType)}/{string.Join(',', method.GetParameters().Select(parameter => Normalize(parameter.ParameterType)))}",
                ConstructorInfo constructor => $"constructor/{member.Name}/{string.Join(',', constructor.GetParameters().Select(parameter => Normalize(parameter.ParameterType)))}",
                FieldInfo field => $"field/{member.Name}/{Normalize(field.FieldType)}",
                PropertyInfo property => $"property/{member.Name}/{Normalize(property.PropertyType)}",
                _ => $"{member.MemberType}/{member.Name}"
            }).Order(StringComparer.Ordinal)];

        static string Normalize(Type type) => (type.FullName ?? type.Name)
            .Replace("Projects.TypedContexts", "Cratis.Screenplay.Contexts", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("return new Total(Guid.NewGuid(), context.Event.Amount);", "Guid.NewGuid")]
    [InlineData("return new Total(Guid.Empty, DateTime.UtcNow.Day);", "DateTime.UtcNow")]
    [InlineData("return new Total(Guid.Empty, Environment.TickCount);", "Environment.TickCount")]
    [InlineData("Environment.ExitCode = 1; return context.State;", "static write")]
    [InlineData("var alias = context; return new Total(Guid.Empty, alias.Tenant.Value.Length);", "context.Tenant alias")]
    [InlineData("return new Total(Guid.Empty, System.IO.File.ReadAllText(\"x\").Length);", "System.IO.File")]
    [InlineData("return new Total(Guid.Empty, new System.Net.Http.HttpClient().Timeout.Seconds);", "HttpClient")]
    [InlineData("return new Total(Guid.Empty, new System.Threading.Thread(() => { }).ManagedThreadId);", "Thread")]
    [InlineData("return new Total(Guid.Empty, System.Threading.Tasks.Task.CompletedTask.Id);", "Task")]
    [InlineData("return new Total(Guid.Empty, System.Threading.Interlocked.Increment(ref number));", "Interlocked")]
    [InlineData("return new Total(Guid.Empty, typeof(Total).GetProperties().Length);", "reflection")]
    [InlineData("return new Total(Guid.Empty, new Random().Next());", "Random")]
    [InlineData("return new Total(Guid.Empty, typeof(Total).GetMethod(\"ToString\")!.MetadataToken);", "reflection metadata")]
    [InlineData("dynamic value = context.Event.Amount; return new Total(Guid.Empty, value);", "dynamic")]
    [InlineData("lock (context) { return context.State; }", "lock")]
    [InlineData("#if DEBUG\nreturn context.State;\n#endif", "directive")]
    [InlineData("return new Total(Guid.Empty, 1.5m.ToString().Length);", "culture formatting")]
    public async Task should_reject_planted_impure_bodies(string body, string category)
    {
        var verdict = await Analyze(body);
        Assert.False(verdict.Accepted, $"Planted {category} violation was admitted.");
    }

    internal static readonly string[] AdmittedFixtures =
    [
            "return new Total(Guid.Empty, context.Event.Amount + 1m);",
            "return new Total(Guid.Empty, Math.Abs(-1m));",
            "return new Total(Guid.Empty, DateOnly.MinValue.AddDays(1).DayNumber);",
            "return new Total(Guid.Empty, \"a\".Contains(\"a\", StringComparison.Ordinal) ? 1m : 0m);",
            "return new Total(Guid.Empty, new List<decimal> { 1m }.Count);",
            "return new Total(Guid.Empty, new[] { 1m }.Sum());",
            "throw new InvalidOperationException();"
    ];

    [Fact]
    public async Task should_exercise_every_allowlist_entry_in_admitted_fixtures()
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fixture in AdmittedFixtures)
        {
            var verdict = await Analyze(fixture);
            Assert.True(verdict.Accepted, $"Fixture {fixture} was refused: {verdict.Code}: {verdict.Reason}");
            used.UnionWith(verdict.UsedAllowlistEntries);
        }

        Assert.Equal(PureTransitionAdmission.AllowlistReasons.Keys.Order(StringComparer.Ordinal), used.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task should_match_the_real_rendered_compilation_verdict_for_every_admitted_fixture()
    {
        var compiled = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var plan = Plan(compiled);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-023" && diagnostic.Message == "1 transition bodies analysed.");
        var descriptor = compiled.TypedContextDescriptors.Single();
        var reducerFile = plan.Artifacts.Single(_ => _.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        const string originalBody = "return new Total(Guid.Empty, context.Event.Amount);";
        foreach (var fixture in AdmittedFixtures)
        {
            var synthetic = await Analyze(fixture);
            var files = plan.Artifacts.Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && _.RelativePath != "Program.cs")
                .Select(artifact => new RenderedFile(artifact.RelativePath,
                    System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace(
                        artifact.RelativePath == reducerFile.RelativePath ? originalBody : "\u0000", fixture, StringComparison.Ordinal)));
            var real = RenderedOutput.CreateCompilation(files);
            var actual = PureTransitionAdmission.AnalyzeRendered(real, reducerFile.RelativePath, descriptor);
            Assert.True(synthetic.Accepted == actual.Accepted,
                $"{fixture}: analysis {synthetic.Code}: {synthetic.Reason}; rendered {actual.Code}: {actual.Reason}; " +
                string.Join("; ", real.GetDiagnostics().Where(_ => _.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)));
            Assert.Equal(synthetic.ContextReads, actual.ContextReads);
            Assert.Equal(synthetic.UsedAllowlistEntries, actual.UsedAllowlistEntries);
        }
    }

    static async Task<PureTransitionAdmission.Verdict> Analyze(string body)
    {
        var loaded = await Load(Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "Guid.Empty", StringComparison.Ordinal));
        var request = new ArtifactRenderRequest(
            loaded.Model,
            loaded.Plan,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        var reducer = context.Reducers.Single();
        var transition = reducer.Transitions.Single();
        return PureTransitionAdmission.Analyze(
            body,
            context,
            context.ReadModels[reducer.ReadModel],
            context.Events[transition.EventContract],
            loaded.TypedContextDescriptors.Single(),
            loaded.ImplementationRequirements.Single());
    }

    internal static string WithIdentifierConcept(string source) => "concept OrderId : Uuid\n" + source
        .Replace("id Uuid identifier", "id OrderId identifier", StringComparison.Ordinal)
        .Replace("id Uuid", "id OrderId", StringComparison.Ordinal);

    internal static async Task<LoadedSemanticModel> Load(string source)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, ".git")) && !Directory.Exists(Path.Combine(root.FullName, ".git")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var folder = Path.Combine(root.FullName, ".ai-work", "stage-namespace", "reducer-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "Orders.play"), source);
            if (source.Contains("file Reducers/Fold.cs", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(Path.Combine(folder, "Reducers"));
                await File.WriteAllTextAsync(Path.Combine(folder, "Reducers", "Fold.cs"), "return new Total(Guid.Empty, context.Event.Amount);");
            }

            return await SemanticModelLoader.LoadFromPathAsync(folder, null, "Projects");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    internal static ArtifactRenderPlan Plan(LoadedSemanticModel compiled)
    {
        var model = compiled.Model;
        var request = new ArtifactRenderRequest(
            model,
            SemanticExecutionPlan.Compile(model).Plan,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = compiled.ImplementationRequirements,
            ImplementationContents = compiled.ImplementationContents,
            TypedContextDescriptors = compiled.TypedContextDescriptors
        };
        return new CratisArtifactRenderPlanner().Plan(request);
    }
}
#endif
