// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticApplicationContext.when_selecting_slices;

public class with_a_domain : given.a_scoped_render_request
{
    IReadOnlyList<string> _paths = null!;

    void Because() => _paths = SelectedPaths(With(ArtifactRenderScopeKind.Feature, _accounts.Id) with
    {
        Profile = DomainPlacementInput.WithDomain(_request.Profile, "Sales/Retail")
    });

    [Fact] void should_preserve_every_domain_segment_in_the_selected_paths() => _paths.ShouldEqual("Sales/Retail/Banking/Accounts/Open", "Sales/Retail/Banking/Accounts/Close", "Sales/Retail/Banking/Accounts/Savings/Save");
}
