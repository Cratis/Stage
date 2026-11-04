// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// What container to create.
/// </summary>
/// <param name="Name">The diagnostic container name. It is never a removal target.</param>
/// <param name="Image">The image to run.</param>
/// <param name="CidFilePath">A private path that does not exist yet, where Docker writes the identifier.</param>
public sealed record GeneratedApplicationContainerDefinition(string Name, string Image, string CidFilePath);
