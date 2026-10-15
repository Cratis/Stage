// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// The exception that is thrown when the framework name catalog text cannot be read.
/// </summary>
/// <param name="line">The one-based line number.</param>
/// <param name="reason">Why the line cannot be read.</param>
internal sealed class MalformedFrameworkNameCatalog(int line, string reason)
    : Exception($"The framework name catalog is malformed at line {line}: {reason}");
