// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_the_protected_read_race_vectors : Specification
{
    JsonArray _vectors = null!;
    SemanticExecutionPlan _plan = null!;
    int _matched;

    void Establish()
    {
        _vectors = JsonNode.Parse(ProtectedCommandReadRaceVectors.Bytes.AsSpan())!["vectors"]!.AsArray();
        _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ProtectedCommandReadRaceVectors.Model.EsmBytes.AsSpan())).Plan!;
    }

    async Task Because()
    {
        foreach (var vector in _vectors)
        {
            await protected_read_race_vectors.Verify(_plan, vector!);
            _matched++;
        }
    }

    [Fact] void should_execute_all_sixteen_vectors_through_the_host() => _matched.ShouldEqual(16);
    [Fact] void should_include_every_published_vector() => _matched.ShouldEqual(_vectors.Count);
}
