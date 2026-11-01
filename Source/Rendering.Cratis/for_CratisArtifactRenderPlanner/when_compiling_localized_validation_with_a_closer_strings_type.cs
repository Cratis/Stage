// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_compiling_localized_validation_with_a_closer_strings_type(when_compiling_localized_validation_with_a_closer_strings_type.context fixture) : IClassFixture<when_compiling_localized_validation_with_a_closer_strings_type.context>
{
    [Fact] void should_compile_the_localized_profile_despite_the_closer_type() => Assert.True(fixture.Compiled);

    public class context : a_generated_application
    {
        public bool Compiled { get; private set; }

        protected override ArtifactRenderPlan CreatePlan()
        {
            var compilation = when_rendering_authoring_documentation.Compile(with_localized_validation.Source.Replace("ProjectRegistered", "GeneratedStrings", StringComparison.Ordinal));
            var model = compilation.Model;
            var profile = CratisRendering.CreateProfile(model.Application.Name, new("Projects", "Projects"), null, new Dictionary<string, string> { ["projects.en.strings"] = "project.name = \"Project name\"\n" }, "en");
            profile = CratisRendering.WithAuthoringMetadata(profile, compilation);
            return new CratisArtifactRenderPlanner().Plan(new(model, SemanticExecutionPlan.Compile(model).Plan!, DomainPlacementInput.WithDomain(profile, "Projects"), new(ArtifactRenderScopeKind.Application, model.Application.Id)));
        }

        async Task Because()
        {
            var command = ReadGeneratedFile(GeneratedFiles("RegisterProject.cs").Single());
            Assert.Contains("using StageImport0 = global::Projects;", command, StringComparison.Ordinal);
            Assert.Contains("StageImport0::GeneratedStrings.Resolve(\"$strings.project.name\")", command, StringComparison.Ordinal);
            Assert.DoesNotContain("global::Projects.GeneratedStrings", command, StringComparison.Ordinal);
            Assert.Contains("namespace Projects.Projects.Projects.Registration.RegisterProject;", command, StringComparison.Ordinal);
            Assert.Contains("public record GeneratedStrings(ProjectId ProjectId, ProjectName Name);", command, StringComparison.Ordinal);

            // The generated event has no Resolve member; compiling this call proves the alias binds to the root strings helper.
            Assert.Contains("namespace Projects;", ReadGeneratedFile("GeneratedStrings.cs"), StringComparison.Ordinal);
            _ = await Run("localized-collision-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Compiled = true;
        }
    }
}
#endif
