// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a Screenplay layout <c>template</c> tree contains a node that is a
/// subtype of <c>TemplateNodeSyntax</c> that <see cref="LayoutConverter"/> does not yet know how to convert.
/// </summary>
/// <param name="typeName">The name of the unrecognized <c>TemplateNodeSyntax</c> subtype.</param>
public class UnknownTemplateNode(string typeName) : Exception($"'{typeName}' is not a known template node");
