// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_passive_read_models : a_generated_application
{
    internal const string Source = """
        command reads protected
        module Storage
          feature Decisions
            slice StateChange Use
              event Stored
                value String
              event Removed
              event Taken
                value String
              command Store
                id String identifier
                value String
                produces Stored
                  for id
                  value = value
              command Consume
                id String identifier
                sourceId String
                reads Current by sourceId
                validate
                  require Current.value != ""
                    message "The stored value must not be empty."
                produces Taken
                  for id
                  value = Current.value
              readmodel Current
                passive
                id String key
                value String
              projection BuildCurrent => Current
                from Stored
                  id = $eventSourceId
                  value = value
                remove with Removed
              specification ReadsTheRequestedStream
                given Stored
                  for "reference"
                  value = "original"
                given Stored
                  for "other"
                  value = "unrelated"
                when Consume
                  id = "target"
                  sourceId = "reference"
                then Taken
                  for "target"
                  value = "original"
              specification RejectsTheEmptyRead
                given Stored
                  for "reference"
                  value = ""
                when Consume
                  id = "target"
                  sourceId = "reference"
                then error "The stored value must not be empty."
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_passive_read_models.Plan(when_rendering_passive_read_models.Compile(Source));

    [Fact]
    async Task should_build_without_chr0046_and_run_the_generated_command_decisions()
    {
        try
        {
            AddGeneratedSpecification("PassiveMetadata.cs", MetadataSpecification);
            var build = await Run("passive-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var tests = await Run("passive-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", tests, StringComparison.Ordinal);
            Assert.DoesNotContain("Failed!", tests, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }

    const string MetadataSpecification = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using Cratis.Chronicle.ReadModels;
        using Projects.Storage.Decisions.Use;
        using Xunit;

        namespace Projects.PassiveMetadata;

        public class when_reading_the_passive_marker
        {
            [Fact]
            public void should_keep_the_generated_read_model_passive() => Assert.True(typeof(Current).IsPassive());
        }
        #endif
        """;
}
#endif
