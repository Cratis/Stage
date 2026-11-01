// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Execution;

[Flags]
internal enum BodyCacheRights
{
    None = 0,
    Write = 1,
    Delete = 2,
    ReplaceChildren = 4,
    Control = 8,
    All = Write | Delete | ReplaceChildren | Control
}
