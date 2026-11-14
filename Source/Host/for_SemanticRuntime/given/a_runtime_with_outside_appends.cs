// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

public class a_runtime_with_outside_appends : a_semantic_runtime
{
    protected const string ProjectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    protected static readonly SemanticValue ProjectKey = SemanticValue.Text(ProjectId);
    private protected a_semantic_fact_log _log = null!;
    protected SemanticKeyedQuery _query = null!;
    an_outside_fact_reader _reader = null!;

    async Task Establish()
    {
        var outside = (SemanticAccepted)await _runtime.Execute(
            _command,
            new Dictionary<string, JsonElement>
            {
                ["projectId"] = JsonSerializer.SerializeToElement(ProjectId),
                ["name"] = JsonSerializer.SerializeToElement("Outside")
            },
            new ClaimsPrincipal(),
            new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"),
            true);
        _log = new();
        _reader = new(_log);
        _runtime = new SemanticRuntime(_runtime.Plan, _log, SemanticWorld.Empty) { FactReader = _reader };
        _query = _runtime.Plan.Queries.Values.Single();
        var initial = (SemanticAccepted)await _runtime.Query(_query, ProjectKey, new ClaimsPrincipal());
        Assert.Empty(initial.Queries.Single().Results);
        _log.Facts.AddRange(outside.Facts);
    }

    protected SemanticValue Name(SemanticReadModelInstance instance) => instance.Values.Single(value => value.TargetProperty == _runtime.Plan.ReadModels.Values.Single().Properties.Single(property => property.Name == "name").Id).Value;

    protected void RefuseOutsideHistory() => _reader.Complete = false;

    void Destroy() => ((SemanticRuntime)_runtime).Dispose();

    sealed class an_outside_fact_reader(a_semantic_fact_log log) : ISemanticFactReader
    {
        public bool Complete { get; set; } = true;

        public Task<ImmutableArray<SemanticFact>> Read(ulong from, ulong through) => Complete ? log.Read(from, through) : Task.FromResult<ImmutableArray<SemanticFact>>([]);
    }
}
