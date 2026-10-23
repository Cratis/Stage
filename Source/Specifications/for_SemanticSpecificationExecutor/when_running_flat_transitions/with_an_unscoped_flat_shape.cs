// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_flat_transitions;

public class with_an_unscoped_flat_shape : given.a_flat_projection
{
    SemanticProjection _flat = null!;
    SemanticProjection _keyless = null!;
    SemanticProjection _unidentified = null!;
    SemanticReadModel _model = null!;

    async Task Because()
    {
        await Run(
            """
            from Claimed key alias
              id = alias
              owner = owner
            """,
            """
            given Claimed
              for "src"
              alias = "a1"
              owner = "bob"
            """,
            """
            id = "a1"
            owner = "bob"
            """);
        _flat = _plan.Projections.Values.Single();
        _model = _plan.ReadModels[_flat.ReadModel];
        var transition = _flat.Transitions.Single();
        _keyless = _flat with { Transitions = [transition with { AffectedInstance = transition.AffectedInstance with { Key = null } }] };
        var identifier = _model.Properties.Single(property => property.IsIdentifier).Id;
        _unidentified = _flat with { Transitions = [transition with { Mappings = [.. transition.Mappings.Where(mapping => mapping.TargetProperty != identifier)] }] };
    }

    [Fact] void should_normalize_the_flat_projection_to_a_scope() => SemanticRunFlatProjections.Normalize(_flat, _model).Scope.From.Single().Mappings.Length.ShouldEqual(2);
    [Fact] void should_drop_the_transitions_it_lowered() => SemanticRunFlatProjections.Normalize(_flat, _model).Transitions.IsEmpty.ShouldBeTrue();
    [Fact] void should_keep_a_flat_shape_without_a_scoped_form() => SemanticRunFlatProjections.Normalize(_keyless, _model).ShouldEqual(_keyless);
    [Fact] void should_keep_a_flat_shape_that_leaves_the_identifier_unmapped() => SemanticRunFlatProjections.Normalize(_unidentified, _model).ShouldEqual(_unidentified);
}
