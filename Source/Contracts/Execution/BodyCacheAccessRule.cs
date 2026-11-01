// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Execution;

internal sealed record BodyCacheAccessRule(
    BodyCachePrincipal Principal,
    BodyCacheRights Rights,
    bool Allow,
    bool InheritOnly,
    bool IsInherited = false);
