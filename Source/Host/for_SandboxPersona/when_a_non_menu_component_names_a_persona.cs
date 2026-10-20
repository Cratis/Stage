// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_a_non_menu_component_names_a_persona : given.a_persona_model
{
    void Because() => _principal = Create(Personas + "\n" + Menu.Replace("Cratis.Blueprint.Default.userMenu", "Cratis.Blueprint.Default.badge"));

    [Fact] void should_use_the_first_persona_instead() => _principal!.FindFirstValue("sub").ShouldEqual("persona:Staff");
    [Fact] void should_use_the_persona_name_without_a_user_menu() => _principal!.Identity!.Name.ShouldEqual("Staff");
}
