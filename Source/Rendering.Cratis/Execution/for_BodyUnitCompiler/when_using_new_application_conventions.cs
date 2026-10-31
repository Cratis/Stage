// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler;

public class when_using_new_application_conventions : Specification
{
    const string OriginalBody = "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);";
    string _directory = null!;

    void Establish() => _directory = Directory.CreateTempSubdirectory("stage-new-application-spec-").FullName;
    void Destroy() => Directory.Delete(_directory, true);

    [Theory]
    [InlineData("Projects")]
    [InlineData("projects")]
    [InlineData("call-for papers")]
    [InlineData("call for papers")]
    [InlineData("CallForPapers")]
    [InlineData("call-for-papers")]
    async Task should_admit_a_concept_constructor_and_retain_the_common_import(string applicationName)
    {
        const string body = "return new Total(Id: new OrderId(Guid.Parse(context.Key)), Amount: context.Event.Amount);";
        var source = when_rendering_a_pure_reducer.WithIdentifierConcept(when_rendering_a_pure_reducer.Source)
            .Replace(OriginalBody, body, StringComparison.Ordinal);
        var path = Path.Combine(_directory, "Model.play");
        await File.WriteAllTextAsync(path, source);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(path, null, applicationName);
        var context = BodyUnitCompiler.CreateContext(loaded);
        Assert.True(context.SelfReduces(context.Reducers.Single().ReadModel));
        if (applicationName == Identifiers.ToPascalCase(applicationName))
        {
            // The planner re-derives the package profile from the raw model name, so it only accepts identifier-named applications; run compilation never plans.
            var rendered = new CratisArtifactRenderPlanner().Plan(context.Request);
            Assert.True(rendered.Success, string.Join(Environment.NewLine, rendered.Diagnostics));
        }

        var entry = Assert.Single(BodyUnitCompiler.Compile(loaded, _directory).Entries);
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        var unit = BodyUnitSources.RenderReducer(BodyUnitCompiler.CreateContext(loaded), Assert.Single(loaded.TypedContextDescriptors), body);
        Assert.Contains($"using {Identifiers.ToPascalCase(applicationName)}.Common;", unit.Sources[0], StringComparison.Ordinal);
        Assert.Contains(body, unit.Sources[0], StringComparison.Ordinal);
        Assert.Equal(unit.EntryType, entry.EntryType);
        Assert.Equal(unit.EntryMethod, entry.EntryMethod);
    }

    [Fact]
    async Task should_refuse_a_reduce_property_like_a_fresh_render()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace(OriginalBody, "return context.State;", StringComparison.Ordinal)
            .Replace("      readmodel Total\n        id Uuid\n        amount Decimal", "      readmodel Total\n        id Uuid\n        reduce Decimal", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        Assert.True(when_rendering_a_pure_reducer.Plan(loaded).Success);
        var rendered = when_rendering_a_pure_reducer.Plan(loaded, self: true);
        Assert.False(rendered.Success);
        var entry = Assert.Single(BodyUnitCompiler.Compile(loaded, _directory).Entries);
        Assert.False(entry.Admitted);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == entry.DiagnosticCode && diagnostic.Message.Contains("reduce", StringComparison.OrdinalIgnoreCase));
    }
}
#endif
