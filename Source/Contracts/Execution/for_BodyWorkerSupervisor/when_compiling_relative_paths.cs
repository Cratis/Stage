// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerSupervisor;

public class when_compiling_relative_paths : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_resolve_model_catalog_and_cache_before_changing_the_child_directory(bool catalog)
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = Directory.CreateTempSubdirectory("stage-worker-path-spec-").FullName;
        try
        {
            var relative = Path.GetRelativePath(Environment.CurrentDirectory, directory);
            await using var supervisor = new BodyWorkerSupervisor(new("unused", relative)
            {
                Launch = new("/bin/sh", ["-c", Script]),
                CompileTimeoutMs = 30000
            });
            await supervisor.CompileAsync(Path.Combine(relative, "Model.play"), catalog ? Path.Combine(relative, "identity.json") : null, "Fixture");
            var request = JsonSerializer.Deserialize<BodyWorkerRequest>(await File.ReadAllTextAsync(Path.Combine(directory, "request.json")), BodyWorkerMessages.Json);
            request.ModelPath.ShouldEqual(Path.Combine(directory, "Model.play"));
            request.CatalogPath.ShouldEqual(catalog ? Path.Combine(directory, "identity.json") : null);
            request.CacheDirectory.ShouldEqual(directory);
            request.ApplicationName.ShouldEqual("Fixture");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    const string Script = """
        printf '%s\n' '{"kind":"hello","protocolVersion":1,"workerVersion":"fixture"}'
        IFS= read -r line
        printf '%s\n' "$line" > request.json
        id=$(printf '%s' "$line" | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')
        printf '{"id":"%s","manifest":{"revision":"fixture","entries":[]}}\n' "$id"
        """;
}
