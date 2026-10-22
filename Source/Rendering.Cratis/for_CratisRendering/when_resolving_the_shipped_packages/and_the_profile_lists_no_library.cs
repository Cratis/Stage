// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_resolving_the_shipped_packages;

public class and_the_profile_lists_no_library
{
    [Fact] void should_declare_the_components_of_the_cratis_component_library() => CratisRendering.ShippedPackages.Single(package => package.Name == "Cratis.Components").Components.ShouldContain("dataTable");
    [Fact] void should_not_see_primereact_as_listed() => CratisRendering.ListsPrimeReact(null).ShouldBeFalse();
    [Fact] void should_not_see_another_library_as_primereact() => CratisRendering.ListsPrimeReact(["Cratis.Components"]).ShouldBeFalse();
    [Fact] void should_see_primereact_when_listed() => CratisRendering.ListsPrimeReact(["core", "PrimeReact"]).ShouldBeTrue();
}
