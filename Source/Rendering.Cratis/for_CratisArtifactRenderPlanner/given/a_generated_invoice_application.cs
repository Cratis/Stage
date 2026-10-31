// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Xml.Linq;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public abstract class a_generated_invoice_application : a_generated_application
{
    public string DebugWarnings { get; private set; } = null!;
    public string ReleaseWarnings { get; private set; } = null!;
    public ImmutableArray<(string Name, string Outcome)> Results { get; private set; }
    public ImmutableArray<GeneratedTestResult> StructuralResults { get; private set; }
    protected abstract string InvoiceSource { get; }

    protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(InvoiceSource));

    protected async Task VerifyGeneratedApplication()
    {
        try
        {
            DebugWarnings = BuildWarnings(await Run("debug-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "-m:3", "--nologo"));
            await Run("debug-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=generated.trx", "--results-directory", _evidence.FullName, "-m:3", "--", "RunConfiguration.MaxCpuCount=3", "xUnit.MaxParallelThreads=3");
            ReleaseWarnings = BuildWarnings(await Run("release-build.log", "build", "InvoiceApp.csproj", "-c", "Release", "-t:Rebuild", "-warnaserror", "-m:3", "--nologo"));
            var results = XDocument.Load(Path.Combine(_evidence.FullName, "generated.trx"));
            StructuralResults = GeneratedTestResults.Read(results);
            Results = [.. StructuralResults.Select(result => ($"{result.ClassName}.{result.MethodName}", result.Outcome))];
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
