// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyCompleteness;

public class when_classifying_root_property_gaps : Specification
{
    readonly SemanticId _root = SemanticId.Create(SemanticKind.Property, "root-event");
    readonly SemanticId _other = SemanticId.Create(SemanticKind.Property, "other-owner");

    [Fact] void should_allow_an_unknown_root_property_reference_kind() => Allowed(new(SemanticDependencyIncompletenessKind.UnknownVariant, _root, "SemanticTypeReference.Kind", _root, "Unparsed reason.")).ShouldBeTrue();
    [Fact] void should_allow_an_unresolved_root_property_target() => Allowed(new(SemanticDependencyIncompletenessKind.UnresolvedIdentity, _root, "SemanticTypeReference.Target", _root, "Unparsed reason.")).ShouldBeTrue();
    [Fact] void should_reject_an_unclassified_member_even_with_root_property_scope() => Allowed(new(SemanticDependencyIncompletenessKind.UnclassifiedMember, _root, "SemanticTypeReference.Target", _root, "Unparsed reason.")).ShouldBeFalse();
    [Fact] void should_reject_duplicate_owners_even_with_root_property_scope() => Allowed(new(SemanticDependencyIncompletenessKind.DuplicateOwner, _root, "SemanticTypeReference.Target", _root, "Unparsed reason.")).ShouldBeFalse();
    [Fact] void should_reject_policy_gaps_even_with_root_property_scope() => Allowed(new(SemanticDependencyIncompletenessKind.UnresolvedPolicy, _root, "SemanticTypeReference.Target", _root, "Unparsed reason.")).ShouldBeFalse();
    [Fact] void should_reject_property_validations_outside_the_type_whitelist() => Allowed(new(SemanticDependencyIncompletenessKind.UnknownVariant, _root, "SemanticProperty.Validations", _root, "Unparsed reason.")).ShouldBeFalse();
    [Fact] void should_reject_absent_owner_provenance() => Allowed(new(SemanticDependencyIncompletenessKind.UnknownVariant, null, "SemanticTypeReference.Kind", _root, "Unparsed reason.")).ShouldBeFalse();
    [Fact] void should_reject_absent_property_type_provenance() => Allowed(new(SemanticDependencyIncompletenessKind.UnknownVariant, _root, "SemanticTypeReference.Kind", null, "Unparsed reason.")).ShouldBeFalse();
    [Fact] void should_reject_mismatched_property_type_ownership() => Allowed(new(SemanticDependencyIncompletenessKind.UnknownVariant, _root, "SemanticTypeReference.Kind", _other, "Unparsed reason.")).ShouldBeFalse();
    [Fact] void should_reject_gaps_on_a_non_root_owner() => Allowed(new(SemanticDependencyIncompletenessKind.UnknownVariant, _other, "SemanticTypeReference.Kind", _other, "Unparsed reason.")).ShouldBeFalse();
    [Fact] void should_reject_non_event_roots() => SemanticDependencyCompleteness.IsRootLocal(new(SemanticDependencyIncompletenessKind.UnknownVariant, _root, "SemanticTypeReference.Kind", _root, "Unparsed reason."), new HashSet<SemanticId> { _root }, new HashSet<SemanticId>()).ShouldBeFalse();
    [Fact] void should_reject_an_unknown_gap_kind() => Allowed(new((SemanticDependencyIncompletenessKind)999, _root, "SemanticTypeReference.Kind", _root, "Unparsed reason.")).ShouldBeFalse();

    bool Allowed(SemanticDependencyIncompleteness gap) => SemanticDependencyCompleteness.IsRootLocal(gap, new HashSet<SemanticId> { _root }, new HashSet<SemanticId> { _root });
}
