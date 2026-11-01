// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_the_canonical_system_identity : a_reaction_runtime
{
    // The corpus starts with a direct append. Add only a command entry point for the live Host;
    // the original reactions, policy and invoked command stay unchanged.
    protected override string Model => Encoding.UTF8.GetString(ReactionIdentityCorpus.SourceForms.Single(form => form.Name == "single").Documents.Single().Bytes.AsSpan())
        .Replace("      command Complete\n", "      command StartWork\n        id String identifier\n        amount Decimal\n        produces Started\n          for id\n          item = id\n      command Complete\n", StringComparison.Ordinal);

    async Task Because() => _result = await Execute();

    [Fact] void should_admit_the_identity_model() => new SemanticRuntimeAdmission(_plan).Blocking.ShouldBeEmpty();
    [Fact] void should_authorize_with_the_declared_system_role() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_the_canonical_follow_up_in_one_batch() => FactNames(_batches.Single()).ToArray().ShouldEqual(["Started", "Completed"]);
}
