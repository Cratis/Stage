// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_a_body_worker_cache_is_untrusted : Specification
{
    [Fact]
    async Task should_keep_rules_opaque_with_a_diagnostic_instead_of_failing_host_startup()
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = Directory.CreateTempSubdirectory("stage-host-cache-spec-").FullName;
        try
        {
            var path = Path.Combine(directory, "Model.play");
            await File.WriteAllTextAsync(path, Model);
            var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
            var cache = Path.Combine(directory, "cache");
            Directory.CreateDirectory(cache, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.OtherWrite);
            var notes = new List<SemanticModelLoadDiagnostic>();
            var settings = new SemanticBodyWorkerOptions(true, Path.Combine(AppContext.BaseDirectory, "bodyworker", "Cratis.Stage.BodyWorker.dll")) { CacheDirectory = cache };
            var bodies = await SemanticBodyWorkerHosting.Start(loaded, path, settings, notes);
            bodies.ShouldBeNull();
            Assert.Single(notes).Code.ShouldEqual(BodyDiagnostics.WorkerFailed);
            Assert.Contains("Body worker failed", notes.Single().Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    const string Model = """
        module M
          feature F
            slice StateChange Add
              command Add
                id String identifier
                amount Int
                validate
                  amount rule Positive message "Must be positive"
                    ```csharp
                    return context.Value > 0;
                    ```
                produces Added
                  for id
                  amount = amount
              event Added
                amount Int
        """;
}
