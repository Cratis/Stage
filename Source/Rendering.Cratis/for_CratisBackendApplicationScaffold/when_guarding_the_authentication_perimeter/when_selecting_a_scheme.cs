// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_selecting_a_scheme : Specification
{
    string _cookie = null!;
    string _bearer = null!;
    string _foreign = null!;
    string _empty = null!;

    void Because()
    {
        _cookie = Select(null);
        _bearer = Select("Bearer <synthetic-token>");
        _foreign = Select("Basic <synthetic-marker>");
        _empty = Select(string.Empty);
    }

    [Fact] void should_select_cookie_without_a_header() => _cookie.ShouldEqual(given.the_emitted_perimeter.Cookie);
    [Fact] void should_select_bearer_with_a_bearer_header() => _bearer.ShouldEqual(given.the_emitted_perimeter.Bearer);
    [Fact] void should_select_bearer_with_a_foreign_scheme() => _foreign.ShouldEqual(given.the_emitted_perimeter.Bearer);
    [Fact] void should_select_bearer_with_an_empty_header() => _empty.ShouldEqual(given.the_emitted_perimeter.Bearer);

    static string Select(string? authorization)
    {
        var context = new DefaultHttpContext();
        if (authorization is not null) context.Request.Headers.Authorization = authorization;
        return given.the_emitted_perimeter.SelectScheme(context);
    }
}
