// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;

public class a_reaction_specification : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    internal const string ProductionSource = """
        module Work
          feature Processing
            slice Automation FollowUp
              event Started
                id String
                amount Decimal
              event Followed
                amount Decimal
              reaction Worker
                when Started
                  produces Followed
                    for id
                    amount = amount
              specification Following
                when append Started
                  id = "one"
                  amount = 5
                then Followed
                  amount = 5
        """;

    internal const string InvocationSource = """
        module Work
          feature Processing
            slice Automation FollowUp
              command Complete
                id String identifier
                produces Completed
                  for id
              event Started
                id String
              event Completed
              reaction Worker
                when Started
                  invokes Complete
                    id = id
              specification Completing
                when append Started
                  id = "one"
                then Completed
        """;

    private protected SemanticSpecification _specification = null!;
    private protected string _source = string.Empty;
    private protected List<ArtifactRenderDiagnostic> _diagnostics = [];

    void Establish() => Prepare(ProductionSource);

    protected void Prepare(string source)
    {
        SetContext(Compile(source));
        _specification = _located.Slice.Specifications.Single();
    }

    protected void PrepareIdentity(string name)
    {
        SetContext(SemanticModelSerializer.Deserialize(ReactionIdentityCorpus.V10.EsmBytes.AsSpan()));
        _specification = _located.Slice.Specifications.Single(specification => specification.Name == name);
    }

    private protected void ChangeReaction(Func<SemanticReaction, SemanticReaction> change) => ReplaceAutomation(_located.Slice with { Reactions = [change(_located.Slice.Reactions.Single())] });

    protected void Render() => _source = SemanticReactionSpecificationRenderer.Render(_specification, _context).Content;

    protected void Validate() => SemanticSpecificationAdmission.Validate(_context, _located.Slice with { Specifications = [_specification] }, _diagnostics);
}
