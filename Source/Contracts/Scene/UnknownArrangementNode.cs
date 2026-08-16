// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a Screenplay <c>arrangement</c> tree contains a node that is a subtype of
/// <c>ArrangementNodeSyntax</c> that <see cref="ArrangementConverter"/> does not yet know how to convert.
/// </summary>
/// <param name="typeName">The name of the unrecognized <c>ArrangementNodeSyntax</c> subtype.</param>
public class UnknownArrangementNode(string typeName) : Exception($"'{typeName}' is not a known arrangement node");
