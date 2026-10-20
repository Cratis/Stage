// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_the_menu_names_an_unknown_persona : given.a_persona_model
{
    void Because() => _principal = Create(Personas + "\n" + Menu.Replace("property role = \"Shopper\"", "property role = \"Missing\""));

    [Fact] void should_use_the_first_persona() => _principal!.FindFirstValue("sub").ShouldEqual("persona:Staff");
    [Fact] void should_keep_the_menu_display_name() => _principal!.Identity!.Name.ShouldEqual("Ada Lovelace");
}
