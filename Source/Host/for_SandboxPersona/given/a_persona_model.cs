// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Stage.Host.for_SandboxPersona.given;

public class a_persona_model : Specification
{
    protected ClaimsPrincipal? _principal;

    protected static ApplicationSyntax Parse(string source)
    {
        var result = new ScreenplayCompiler().Compile(source);
        result.Diagnostics.ShouldBeEmpty();
        result.Success.ShouldBeTrue();
        return result.Value!;
    }

    protected static ClaimsPrincipal? Create(string source) => SandboxPersona.Create(Parse(source), NullLogger.Instance);

    protected const string Personas = """
        policy IsStaff
          require role "Staff"
        policy IsShopper
          require authenticated and role "Shopper" and claim "department" matches "shopping"
        persona Staff
          policy IsStaff
        persona Shopper
          policy IsShopper
        """;

    protected const string Menu = """
        module Shop
          screen template ShopShell
            category Shell
            scopes application
            topbar
            content topbar
              component Cratis.Blueprint.Default.userMenu account
                property name = "Ada Lovelace"
                property role = "Shopper"
        """;
}
