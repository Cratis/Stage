// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// Screenplay 4.133.3's positive typed composition vector includes navigation metadata that the pinned Scene
/// runtime does not honor. Refuse the whole translation rather than pinning bytes that silently lose that metadata.
/// </summary>
public class when_translating_the_typed_screen_composition : given.the_planned_screen_composition_corpus
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => SceneOf([ScreenCompositionCorpus.V1.TypedSourceCases.Single(_ => _.Name == "screen-release-ui-positive").Document]));

    [Fact] void should_refuse_the_composition() => _error.ShouldBeOfExactType<UnsupportedUiSyntax>();
    [Fact] void should_report_the_typed_refusal_code() => _error.Message.ShouldContain(UnsupportedUiSyntax.DiagnosticCode);
    [Fact] void should_name_the_first_unsupported_navigation_field() => ((UnsupportedUiSyntax)_error).Member.ShouldEqual("ContributionSyntax.Id");
    [Fact] void should_explain_the_runtime_limitation() => _error.Message.ShouldContain("The pinned Scene runtime does not honor this field.");
}
