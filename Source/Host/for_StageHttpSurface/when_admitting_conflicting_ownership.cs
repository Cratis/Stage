// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageHttpSurface;

public class when_admitting_conflicting_ownership : given.conflicting_surfaces
{
    readonly Dictionary<string, Exception?> _failures = [];
    readonly Dictionary<string, Exception?> _reversed = [];

    void Because()
    {
        foreach (var (name, model) in _models)
        {
            _failures[name] = Catch.Exception(() => StageHttpSurface.Create(model));
            _reversed[name] = Catch.Exception(() => StageHttpSurface.Create(given.RouteModels.Reverse(model)));
        }
    }

    [Fact] void should_reject_case_collisions() => _failures["case"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_reject_kebab_collisions() => _failures["kebab"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_reject_sanitization_collisions() => _failures["sanitization"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_reject_empty_identifier_collisions() => _failures["empty identifier"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_reject_query_collisions_before_arc_can_deduplicate_them() => _failures["query normalization"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_reject_omitted_collection_identity_before_the_type_cache_can_fold_it() => _failures["omitted collection"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_reject_a_command_and_read_model_sharing_a_clr_name_even_with_different_http_methods() => _failures["shared type cache"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_reject_cross_depth_execute_versus_validate() => _failures["execute versus validate"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_not_repurpose_a_foreign_singleton_legacy_url() => _failures["canonical versus singleton alias"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_not_repurpose_an_ambiguous_foreign_legacy_url() => _failures["canonical versus ambiguous alias"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_reject_a_foreign_cross_depth_query_alias() => _failures["query cross depth alias"].ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_report_the_same_failure_under_reversed_model_order() => _failures.All(pair => pair.Value?.Message == _reversed[pair.Key]?.Message).ShouldBeTrue();
    [Fact] void should_report_method_and_normalized_path() => _failures.Values.Cast<AmbiguousStageHttpSurface>().All(failure => failure.Message.Contains($"{failure.Method} {failure.Path}", StringComparison.Ordinal) && failure.Path.StartsWith("/api/orders/checkout", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_slice_ids_and_qualified_artifacts() => _failures.Values.All(failure => failure!.Message.Contains("slice '", StringComparison.Ordinal) && failure.Message.Contains("artifact 'Stage.", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_identify_execute_and_validate_owners() => (_failures["execute versus validate"]!.Message.Contains("Execute slice", StringComparison.Ordinal) && _failures["execute versus validate"]!.Message.Contains("Validate slice", StringComparison.Ordinal)).ShouldBeTrue();
}
