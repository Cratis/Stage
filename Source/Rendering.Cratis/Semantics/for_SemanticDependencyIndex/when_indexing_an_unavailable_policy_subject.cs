// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_indexing_an_unavailable_policy_subject : given.a_dependency_index
{
    SemanticId _operation;

    void Establish()
    {
        var command = _registerProject.Commands.Single() with { Authorization = new SemanticPolicyReference("Access") };
        _operation = command.Id;
        _registerProject = _registerProject with { Commands = [command] };
        _application = _application with { Policies = [new("Access", new SemanticOpaquePolicyCondition("policy-body"))] };
        _request = _request with
        {
            TypedContextDescriptors = [new("policy-body", SemanticImplementationRole.PolicyPredicate, 1, _operation,
            [
                new("Artifact", new(SemanticContextTypeKinds.Shape, null, _operation, null), false, false, new(SemanticContextSourceKinds.AuthorizedOperation, _operation, "Artifact")),
                new("Subject", new(SemanticContextTypeKinds.Runtime, null, null, SemanticContextRuntimeTokens.Text), true, false, new(SemanticContextSourceKinds.Unavailable, null, string.Empty)),
                Runtime("Identity", SemanticContextRuntimeTokens.Identity),
                Runtime("Tenant", SemanticContextRuntimeTokens.TenantId),
                Runtime("Occurred", SemanticContextRuntimeTokens.DateTime)
            ])]
        };
    }

    static SemanticTypedContextMember Runtime(string name, string token) => new(name, new(SemanticContextTypeKinds.Runtime, null, null, token), false, false, new(SemanticContextSourceKinds.ContextContract, null, name));

    void Because() => BuildIndex();

    [Fact] void should_cover_a_policy_subject_without_a_semantic_identity() => Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));
    [Fact] void should_preserve_the_policy_dependency() => _index.Edges.Any(edge => edge.Kind == SemanticDependencyKind.Policy && edge.Dependent == _operation && edge.Dependency == _application.Id).ShouldBeTrue();
    [Fact] void should_not_invent_a_subject_dependency() => _index.Edges.Any(edge => edge.Member == "SemanticContextSource.SemanticId" && edge.Dependent == _operation && edge.Dependency != _operation).ShouldBeFalse();
}
