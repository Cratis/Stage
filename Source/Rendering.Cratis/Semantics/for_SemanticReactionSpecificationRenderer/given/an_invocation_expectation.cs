// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;

public class an_invocation_expectation : a_reaction_specification
{
    void Establish()
    {
        SetContext(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(34).Bytes.AsSpan()));
        _specification = _located.Slice.Specifications.Single();
    }

    private protected void SetExpectations(ImmutableArray<SemanticSpecificationInvocation> expectations)
    {
        _specification = _specification with { ThenInvocations = expectations };
    }

    private protected void ChangeTrigger(Func<SemanticReactionTrigger, SemanticReactionTrigger> change) => ChangeReaction(reaction => reaction with { Triggers = [change(reaction.Triggers.Single())] });

    protected void ValidateExpectations() => SemanticSpecificationAdmission.ValidateInvocations(_context, _located.Slice with { Specifications = [_specification] }, _diagnostics);
}
#endif
