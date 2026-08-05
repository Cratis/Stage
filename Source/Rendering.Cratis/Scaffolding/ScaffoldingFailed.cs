// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// The exception that is thrown when the template engine fails to install a template package or instantiate a
/// template.
/// </summary>
/// <param name="reason">The reason scaffolding failed.</param>
public class ScaffoldingFailed(string reason) : Exception(reason);
