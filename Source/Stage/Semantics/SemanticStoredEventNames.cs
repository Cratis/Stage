// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

internal static class SemanticStoredEventNames
{
    internal static string Of(SemanticEventContract contract) => contract.StoredName ?? contract.Name;
}
