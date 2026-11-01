// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Execution;

internal enum BodyCachePrincipal
{
    Other,
    CurrentUser,
    System,
    Administrators,
    CreatorOwner,
    CreatorGroup
}
