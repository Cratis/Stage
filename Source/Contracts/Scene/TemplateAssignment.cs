// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// A template a Screenplay document assigns or uses, as Scene's <see cref="SceneScreens.TemplateReference"/>: an own template by name,
/// or a template a blueprint package ships with the semver range of the blueprint the author accepts.
/// </summary>
/// <param name="Scope">Where it is assigned: <c language="csharp">application</c>, <c language="csharp">module</c>, <c language="csharp">feature</c>, <c language="csharp">slice</c> or <c language="csharp">screen</c>.</param>
/// <param name="Owner">The dotted path of the module, feature, slice or screen it belongs to; empty for the application.</param>
/// <param name="Reference">The Scene reference - source, id, and for a blueprint template its blueprint and version range.</param>
public record TemplateAssignment(string Scope, string Owner, SceneScreens.TemplateReference Reference);
