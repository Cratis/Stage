// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_handler_specifications.given;

public abstract class a_handler_application : a_generated_application
{
    public string Warnings { get; private set; } = null!;
    public (string Name, string Outcome)[] Results { get; private set; } = [];
    protected virtual string Body => handler_model.Body;

    protected override ArtifactRenderPlan CreatePlan() => handler_model.Plan(handler_model.Request(Body, with_a_body_dependent_specification.Source));

    protected async Task ExecuteSpecifications(bool allowFailedTests = false)
    {
        try
        {
            Warnings = BuildWarnings(await Run("debug-build.log", "build", "HandlerApp.csproj", "-c", "Debug", "-warnaserror", "--nologo"));
            await Run("debug-test.log", allowFailedTests, "test", "HandlerApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=handler.trx", "--results-directory", _evidence.FullName);
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            Results = [.. XDocument.Load(Path.Combine(_evidence.FullName, "handler.trx")).Descendants(ns + "UnitTestResult").Select(result => (result.Attribute("testName")!.Value, result.Attribute("outcome")!.Value))];
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
