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
    protected abstract string InvoiceSource { get; }

    protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(InvoiceSource));

    protected async Task VerifyGeneratedApplication()
    {
        try
        {
            DebugWarnings = BuildWarnings(await Run("debug-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "--nologo"));
            await Run("debug-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=generated.trx", "--results-directory", _evidence.FullName);
            ReleaseWarnings = BuildWarnings(await Run("release-build.log", "build", "InvoiceApp.csproj", "-c", "Release", "-t:Rebuild", "-warnaserror", "--nologo"));
            var results = XDocument.Load(Path.Combine(_evidence.FullName, "generated.trx"));
            XNamespace testNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            Results = [.. results.Descendants(testNamespace + "UnitTestResult").Select(_ => (_.Attribute("testName")!.Value, _.Attribute("outcome")!.Value))];
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
