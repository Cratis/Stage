// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// Where the value of a part of an HTTP request or of an ability's returned value comes from.
/// </summary>
public enum HttpValueKind
{
    /// <summary>A string, number or Boolean written in the block.</summary>
    Literal = 0,

    /// <summary>A property of the call itself: <c lang="text">$call.idempotencyKey</c>, <c lang="text">$call.attempt</c>, <c lang="text">$call.tenant</c>, <c lang="text">$call.system</c> or <c lang="text">$call.ability</c>.</summary>
    Call = 1,

    /// <summary>An input of the ability, optionally a path into it such as <c lang="text">address.street</c>.</summary>
    Input = 2,

    /// <summary>A path into the body of the response, such as <c lang="text">body.tracking</c>, or <c lang="text">status</c>.</summary>
    Response = 3
}
