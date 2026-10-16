// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents the scaffolded projects a rendered C# file can compile into. The application project builds with
/// <c language="csharp">Microsoft.NET.Sdk.Web</c> and the specifications project with <c language="csharp">Microsoft.NET.Sdk</c>, so
/// the names in scope differ between them.
/// </summary>
[Flags]
internal enum GeneratedProjects
{
    /// <summary>
    /// No project.
    /// </summary>
    None = 0,

    /// <summary>
    /// The application project.
    /// </summary>
    Application = 1,

    /// <summary>
    /// The specifications project, which references the application project.
    /// </summary>
    Specifications = 2,

    /// <summary>
    /// Both projects.
    /// </summary>
    Both = Application | Specifications
}
