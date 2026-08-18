// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Screens;

/// <summary>
/// Represents a read model a screen binds to - the modeled <c>data</c> directive.
/// </summary>
/// <param name="ReadModel">The name of the read model the screen shows.</param>
/// <param name="Query">The name of the query providing the data.</param>
/// <param name="By">The parameter the query is keyed by, or <see langword="null"/> when it is not keyed.</param>
public record ScreenDataBinding(string ReadModel, string Query, string? By);

/// <summary>
/// Represents a command a screen exposes - the modeled <c>action</c> directive.
/// </summary>
/// <param name="Command">The name of the command the action invokes.</param>
/// <param name="Label">The display label, or <see langword="null"/> when none is declared.</param>
/// <param name="NavigatesTo">The name of the screen navigated to once the action succeeds, or
/// <see langword="null"/> when it navigates nowhere.</param>
public record ScreenAction(string Command, string? Label, string? NavigatesTo);

/// <summary>
/// Represents a <c>screen</c> declared within a slice - which read models it shows, and which commands it
/// offers.
/// </summary>
/// <param name="Id">The unique identifier of the screen.</param>
/// <param name="Name">The name of the screen.</param>
/// <param name="File">The relative path of the file the screen lives in, or an empty string when the screen is
/// declared inline.</param>
/// <param name="Data">The read models the screen binds to, in declaration order.</param>
/// <param name="Actions">The commands the screen offers, in declaration order.</param>
/// <remarks>
/// What is carried here is the screen's place in the event model - the read models it reads and the commands
/// it sends - and deliberately not its presentation, which is translated in whole by
/// <see cref="EventModelLoader.LoadSceneApplicationFromDirectoryAsync"/> into
/// <see cref="Scene.SceneApplication"/>, a parallel output of the same source rather than a part of this one.
/// <para>
/// That parallel output is not presentation-only, and this record does not fill a gap in it.
/// <see cref="Scene.ScreenDirectiveConverter"/> reads the same <c>data</c> and <c>action</c> directives, and
/// on their fields carries more than this does - <c>isCollection</c> and <c>navigateByParameter</c> appear
/// there and not here - while <see cref="Scene.ScreenConverter"/> also resolves a screen's forms from the
/// commands its actions reference. The two therefore do give the same source two encodings of the same
/// information, and what this one contributes is the shape rather than the content:
/// <see cref="Scene.SceneApplication"/> holds it as untyped <c>object?</c> entries in the property bag of
/// <c>core:data</c> and <c>core:action</c> components nested in a screen's slot content, reachable only by
/// walking an element tree and matching component names as strings, where <c>Data</c> and <c>Actions</c> are
/// typed lists.
/// </para>
/// <para>
/// Bindings and actions are collected from the whole screen body, however deeply a section, slot or template
/// reference nests them, so nesting changes where they appear on the screen and never whether they appear
/// here.
/// </para>
/// </remarks>
public record ScreenDefinition(
    Guid Id,
    string Name,
    string File,
    IReadOnlyList<ScreenDataBinding> Data,
    IReadOnlyList<ScreenAction> Actions);
