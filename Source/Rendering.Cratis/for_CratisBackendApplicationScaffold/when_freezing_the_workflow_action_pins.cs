// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_freezing_the_workflow_action_pins : Specification
{
    [Fact] void should_pin_checkout() => CratisContinuousIntegrationActions.Checkout.ShouldEqual("actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1");
    [Fact] void should_pin_setup_dotnet() => CratisContinuousIntegrationActions.SetupDotnet.ShouldEqual("actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0");
    [Fact] void should_pin_setup_node() => CratisContinuousIntegrationActions.SetupNode.ShouldEqual("actions/setup-node@820762786026740c76f36085b0efc47a31fe5020 # v7.0.0");
    [Fact] void should_build_the_frontend_with_node_24() => CratisContinuousIntegrationActions.NodeVersion.ShouldEqual("24");
}
