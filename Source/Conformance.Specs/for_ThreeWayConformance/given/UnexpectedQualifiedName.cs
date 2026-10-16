// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

/// <summary>
/// The exception that is thrown when a qualified type name has a syntax shape the rewrite does not know.
/// </summary>
/// <param name="name">The qualified name.</param>
sealed class UnexpectedQualifiedName(string name) : Exception($"The qualified name '{name}' has an unexpected syntax shape.");
#endif
