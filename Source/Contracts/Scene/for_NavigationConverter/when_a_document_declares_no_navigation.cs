// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_NavigationConverter;

public class when_a_document_declares_no_navigation : given.a_translated_application
{
    void Because() => Translate(Model);

    [Fact] void should_have_no_application_regions() => _application.Navigation.Application.Regions.ShouldBeEmpty();
    [Fact] void should_have_no_logo() => _application.Navigation.Application.Logo.ShouldBeNull();
    [Fact] void should_have_no_module_navigation() => _application.Navigation.Modules.ShouldBeEmpty();
}
