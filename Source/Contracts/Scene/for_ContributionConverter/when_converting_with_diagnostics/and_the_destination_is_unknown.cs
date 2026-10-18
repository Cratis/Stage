// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ContributionConverter.when_converting_with_diagnostics;

public class and_the_destination_is_unknown : Specification
{
    [Fact]
    public void should_keep_the_contribution_and_report_the_omitted_destination()
    {
        var location = new SourceLocation(6, 3, "navigation.play");
        var contribution = new ContributionSyntax("Navigation", null, "Retained", 3, location) { Destination = new((ContributionDestinationKind)99, "unknown", location) };
        var diagnostics = new ConversionDiagnostics();
        var result = ContributionConverter.Convert(contribution, "Projects.navigation", ["Projects"], diagnostics);

        result.Content.Properties["label"].ShouldEqual("Retained");
        result.Content.Properties.ContainsKey(SceneElementProperties.Destination).ShouldBeFalse();
        diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-CONTRIBUTION-001");
        diagnostics.All.Single().Location.ShouldEqual(location);
        diagnostics.All.Single().Path.ShouldEqual("Projects.navigation");
        Catch.Exception(() => ContributionConverter.Convert(contribution, "Projects.navigation")).ShouldBeOfExactType<UnknownCompositionValue>();
    }
}
