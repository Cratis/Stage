// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Xml.Linq;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// A generated application whose public event and shared concept moved into a separate contracts project, planned through the public
/// <see cref="CratisRendering.Plan(ExecutableSemanticModel, SemanticExecutionPlan, ArtifactRenderScope, CratisRenderingOptions)"/>, written unchanged
/// under the configured proof root and built for real in one configuration.
/// </summary>
public sealed class rendered_event_contracts
{
    /// <summary>The variable that opts in to the expensive generated-application builds; the value must be 1.</summary>
    public const string EnableVariable = "STAGE_EVENT_CONTRACTS_BUILD";

    /// <summary>The variable naming the directory under which the proof keeps its cases and diagnostics.</summary>
    public const string RootVariable = "STAGE_EVENT_CONTRACTS_PROOF_ROOT";

    const string ApplicationName = "ContractsOwner";
    const string ProjectName = "ContractsProofApp";
    const string ContractsFolder = ProjectName + ".Events.Contracts";
    const string Framework = "net10.0";

    const string Source = """
        concept OrderName : String
        module Shop
          feature Sales
            slice StateChange Orders
              event Drafted
                name OrderName
              command Draft
                id String identifier
                name OrderName
                produces Drafted
                  for id
                  name = name
            slice Translate Publish
              direction outbound
              public event OrderPublished
                name OrderName
              projection OrderPublisher => OrderPublished
                from Drafted
                  name = name
        """;

    static readonly TimeSpan _buildDeadline = TimeSpan.FromSeconds(240);
    static readonly TimeSpan _fixtureBudget = TimeSpan.FromSeconds(270);

    readonly string _configuration;
    readonly bool _eventTargetReducer;
    readonly string _case;
    readonly string _application;
    ArtifactRenderPlan _plan = null!;
    ProcessResult _build = null!;
    DateTime _startedUtc;

    /// <summary>
    /// Initializes a new instance of the <see cref="rendered_event_contracts"/> class and allocates its fresh case directory.
    /// </summary>
    /// <param name="configuration">The generated application's build configuration, Debug or Release.</param>
    /// <param name="eventTargetReducer">Whether publication is performed by a native event-target reducer.</param>
    public rendered_event_contracts(string configuration, bool eventTargetReducer = false)
    {
        _configuration = configuration;
        _eventTargetReducer = eventTargetReducer;
        _case = Path.Combine(ProofRoot(), $"{configuration.ToLowerInvariant()}-{Guid.NewGuid():N}");
        _application = Path.Combine(_case, "application");
        Directory.CreateDirectory(_application);
        Log($"case {_case} configuration {configuration}");
    }

    string ProcessLog => Path.Combine(_case, "build.log");

    string BinaryLog => Path.Combine(_case, "build.binlog");

    string Evidence => $"{Environment.NewLine}Case directory (retained): {_case}{Environment.NewLine}Build log: {ProcessLog}";

    /// <summary>
    /// Binds the model, plans it through the public API, writes every artifact unchanged and checks the emitted project graph.
    /// </summary>
    public void Render()
    {
        var source = _eventTargetReducer ? Source.Replace("      projection OrderPublisher => OrderPublished\n        from Drafted\n          name = name", "      reducer OrderPublisher => OrderPublished\n        on Drafted\n          ```csharp\n          return new OrderPublished(context.Event.Name);\n          ```", StringComparison.Ordinal) : Source;
        File.WriteAllText(Path.Combine(_case, "model.play"), source);
        var loaded = _eventTargetReducer ? SemanticModelLoader.LoadFromPathAsync(_case, null, ApplicationName).GetAwaiter().GetResult() : null;
        var model = loaded?.Model ?? Compile();
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, "The execution plan did not compile: " + string.Join(Environment.NewLine, execution.Issues.Select(issue => issue.Details)) + Evidence);
        _plan = loaded is null ? CratisRendering.Plan(
            model,
            execution.Plan!,
            new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id),
            new CratisRenderingOptions(ProjectName, "ContractsProof")) : CratisRendering.Plan(
                model,
                execution.Plan,
                new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id),
                new CratisRenderingOptions(ProjectName, "ContractsProof"),
                loaded.ImplementationRequirements,
                loaded.ImplementationContents,
                [],
                loaded.TypedContextDescriptors);
        Assert.True(_plan.Success, "The plan was refused: " + string.Join(Environment.NewLine, _plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")) + Evidence);
        Assert.Empty(_plan.Refusals);
        Assert.NotEmpty(_plan.Artifacts);
        Assert.Equal(ApplicationName, _plan.ApplicationName);
        Write();
        AssertProjectGraph();
        Log("rendered and checked the emitted project graph");
    }

    /// <summary>
    /// Builds the emitted application project, which builds the contracts project it references, in this configuration.
    /// </summary>
    /// <returns>A task that completes when the build ended or its deadline passed.</returns>
    public async Task Build()
    {
        _startedUtc = DateTime.UtcNow;
        Log($"building {ProjectName}.csproj ({_configuration})");
        using var budget = new CancellationTokenSource(_fixtureBudget);
        _build = await ApplicationProcess.RunLogged(
            new LoggedTool(
                _application,
                _buildDeadline,
                ProcessLog,
                "dotnet",
                [
                    "build",
                    $"{ProjectName}.csproj",
                    "--configuration",
                    _configuration,
                    "--disable-build-servers",
                    "--nologo",
                    "--verbosity",
                    "minimal",
                    "-m:3",
                    "-p:UseSharedCompilation=false",
                    "-warnaserror",
                    $"-bl:{BinaryLog}"
                ]),
            budget.Token);
        Log($"build ended with exit code {_build.ExitCode}");
    }

    /// <summary>
    /// Asserts that the build succeeded without warnings and produced the application, the contracts project and the contracts assembly beside the application.
    /// </summary>
    public void AssertBuilt()
    {
        Assert.NotNull(_build);
        Assert.True(_build.ExitCode == 0, $"The generated {_configuration} application did not build (exit code {_build.ExitCode})." + Evidence + Environment.NewLine + Tail(_build.Output));
        Assert.DoesNotContain(": warning ", _build.Output);
        var applicationOutput = Path.Combine(_application, "bin", _configuration, Framework);
        var contractsOutput = Path.Combine(_application, ContractsFolder, "bin", _configuration, Framework);
        AssertFresh(Path.Combine(applicationOutput, $"{ProjectName}.dll"));
        AssertFresh(Path.Combine(contractsOutput, $"{ContractsFolder}.dll"));
        AssertFresh(Path.Combine(applicationOutput, $"{ContractsFolder}.dll"));
        Log("asserted the built outputs");
    }

    /// <summary>
    /// Removes this case's generated application tree after a passing proof; the case's diagnostics stay.
    /// </summary>
    public void Complete()
    {
        Directory.Delete(_application, recursive: true);
        Log("removed the generated application tree");
    }

    static string ProofRoot()
    {
        var configured = Environment.GetEnvironmentVariable(RootVariable);
        Assert.False(string.IsNullOrWhiteSpace(configured), $"{RootVariable} must name an existing proof directory when {EnableVariable} is 1.");
        Assert.True(Path.IsPathFullyQualified(configured), $"{RootVariable} must be an absolute path, not {configured}.");
        var root = Path.GetFullPath(configured).TrimEnd(Path.DirectorySeparatorChar);
        Assert.True(Directory.Exists(root), $"{RootVariable} names {root}, which does not exist.");
        Assert.True(new DirectoryInfo(root).LinkTarget is null, $"{root} is a symbolic link; name the directory it points to.");
        return root;
    }

    static ExecutableSemanticModel Compile()
    {
        var syntax = new ScreenplayCompiler().Parse(Source);
        Assert.True(syntax.Success, string.Join(';', syntax.Diagnostics));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", Source);
        var bound = new SemanticModelBinder().Bind(ApplicationName, syntax.Value!, SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join(';', bound.Diagnostics));
        return bound.Value!.Model;
    }

    static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());

    static bool InContracts(PlannedArtifact artifact) => artifact.RelativePath.StartsWith($"{ContractsFolder}/", StringComparison.Ordinal);

    static bool IsSource(PlannedArtifact artifact) => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal);

    static string Tail(string output) => output.Length <= 4000 ? output : output[^4000..];

    void Write()
    {
        var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var artifact in _plan.Artifacts)
        {
            var relativePath = artifact.RelativePath;
            Assert.False(string.IsNullOrEmpty(relativePath) || Path.IsPathRooted(relativePath) || relativePath.Contains('\\'), $"Unsafe artifact path '{relativePath}'.");
            Assert.DoesNotContain(relativePath.Split('/'), segment => segment.Length == 0 || string.Equals(segment, ".", StringComparison.Ordinal) || string.Equals(segment, "..", StringComparison.Ordinal));
            var destination = Path.GetFullPath(Path.Combine(_application, relativePath));
            Assert.StartsWith(_application + Path.DirectorySeparatorChar, destination, StringComparison.Ordinal);
            Assert.True(written.Add(destination), $"Duplicate artifact destination '{relativePath}'.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllBytes(destination, [.. artifact.Bytes]);
        }
    }

    void AssertProjectGraph()
    {
        var projects = _plan.Artifacts.Select(artifact => artifact.RelativePath).Where(path => path.EndsWith(".csproj", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        string[] expected = [$"{ContractsFolder}/{ContractsFolder}.csproj", $"{ProjectName}.csproj"];
        Assert.Equal(expected, projects);

        var contracts = XDocument.Parse(Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == $"{ContractsFolder}/{ContractsFolder}.csproj")));
        var owner = contracts.Descendants("AssemblyAttribute").Single(element => (string?)element.Attribute("Include") == "Cratis.Chronicle.Events.EventStoreAttribute");
        Assert.Equal(ApplicationName, owner.Element("_Parameter1")!.Value);

        var application = XDocument.Parse(Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == $"{ProjectName}.csproj")));
        Assert.Single(application.Descendants("ProjectReference"), element => (string?)element.Attribute("Include") == $"{ContractsFolder}/{ContractsFolder}.csproj");
        Assert.Contains(application.Descendants("DefaultItemExcludes"), element => element.Value.Contains($"{ContractsFolder}/**", StringComparison.Ordinal));

        var sources = _plan.Artifacts.Where(IsSource).ToArray();
        Assert.True(InContracts(Assert.Single(sources, artifact => Text(artifact).Contains("record OrderPublished(", StringComparison.Ordinal))), "The public event is not in the contracts project.");
        Assert.True(InContracts(Assert.Single(sources, artifact => Text(artifact).Contains("record OrderName(", StringComparison.Ordinal))), "The shared concept is not in the contracts project.");
        Assert.DoesNotContain(sources.Where(InContracts), artifact => Text(artifact).Contains("record Drafted(", StringComparison.Ordinal));
        Assert.Contains(sources.Where(artifact => !InContracts(artifact)), artifact => Text(artifact).Contains("OrderName", StringComparison.Ordinal));
    }

    void AssertFresh(string path)
    {
        Assert.True(File.Exists(path), $"The build did not produce {path}." + Evidence);
        Assert.True(File.GetLastWriteTimeUtc(path) >= _startedUtc.AddSeconds(-5), $"{path} predates this build." + Evidence);
    }

    void Log(string message) =>
        File.AppendAllText(Path.Combine(_case, "lifecycle.log"), $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}");
}

/// <summary>
/// A fact that is skipped, visibly, unless the expensive generated-application build proof was opted in to.
/// </summary>
public sealed class EventContractsBuildFactAttribute : FactAttribute
{
    /// <inheritdoc/>
    public override string? Skip
    {
        get => string.Equals(Environment.GetEnvironmentVariable(rendered_event_contracts.EnableVariable), "1", StringComparison.Ordinal)
            ? null
            : $"{rendered_event_contracts.EnableVariable} is not 1; this proof builds a generated application and is opt-in.";
        set => base.Skip = value;
    }
}
#endif
