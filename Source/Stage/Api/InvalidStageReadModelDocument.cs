// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Api;

/// <summary>
/// The exception that is thrown when Chronicle returns a read model document Stage cannot parse.
/// </summary>
/// <param name="readModelIdentifier">The registered read model identifier.</param>
/// <param name="innerException">The parsing error, when available.</param>
public sealed class InvalidStageReadModelDocument(string readModelIdentifier, Exception? innerException = null)
    : Exception($"Chronicle returned an invalid document for read model '{readModelIdentifier}'.", innerException);
