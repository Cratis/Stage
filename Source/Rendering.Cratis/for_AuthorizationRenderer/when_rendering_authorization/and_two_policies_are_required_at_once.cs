// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.when_rendering_authorization;

/// <summary>
/// Pins the behavior of a conjunction — <c>authorize Administrator and Auditor</c>, which is also what writing
/// two policies next to each other means.
/// </summary>
/// <remarks>
/// <b>This spec asserts an answer that is known to be wrong</b>, so that fixing it is a deliberate change to a
/// spec rather than a silent change in rendered output — see
/// <see href="https://github.com/Cratis/Stage/issues/20">Cratis/Stage#20</see>. The document demands <i>both</i>
/// policies; <c>[Roles]</c> grants on <i>any one</i> of the roles it lists, so the rendered application is more
/// permissive than declared, and says nothing about it. When
/// <see href="https://github.com/Cratis/Arc/issues/2464">Cratis/Arc#2464</see> makes a named policy actually
/// evaluate, both facts below should flip: the attribute should demand both, and the silence should become a
/// diagnostic if it still cannot.
/// </remarks>
public class and_two_policies_are_required_at_once : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = Render(AuthorizeAll("Administrator", "Auditor"));

    [Fact] void should_admit_either_role_although_the_document_demands_both() =>
        _attribute.ShouldEqual("Roles(\"Administrator\", \"Auditor\")");
    [Fact] void should_not_report_that_it_renders_something_weaker_than_declared() => _diagnostics.ShouldBeEmpty();
}
