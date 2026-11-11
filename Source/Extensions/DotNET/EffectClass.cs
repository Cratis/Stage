// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Extensions;

/// <summary>
/// How an ability behaves when it is performed more than once, which decides what Stage may do after an unknown outcome.
/// </summary>
public enum EffectClass
{
    /// <summary>
    /// Performing it again changes nothing. Stage retries freely.
    /// </summary>
    Safe = 0,

    /// <summary>
    /// Performing it again with the same idempotency key has the same effect as once. Stage retries.
    /// </summary>
    Idempotent = 1,

    /// <summary>
    /// The effect must happen at most once. After an unknown outcome Stage stops for reconciliation instead of retrying.
    /// </summary>
    Once = 2
}
