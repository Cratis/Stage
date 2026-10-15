// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.given;

/// <summary>
/// Compiles Screenplay source, and translates what it compiled into Scene.
/// </summary>
public class a_translated_application : Specification
{
    /// <summary>
    /// A shell, an Orders feature with a State Change slice, a State View slice with a screen, and an Automation, to which
    /// scenarios append navigation and toolbar lines.
    /// </summary>
    protected const string Model =
        """
        module Shop
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              screen List
                title "Orders"
            slice Automation Notify
              reaction Notifier
                when OrderPlaced
          feature Returns
            slice StateChange StartReturn
              command StartReturn
                returnId Uuid identifier
                produces ReturnStarted
                  returnId = returnId
              event ReturnStarted
                returnId Uuid
        module Support
          feature Tickets
        module Empty
        """;

    protected SceneApplication _application = null!;
    protected IReadOnlyList<Cratis.Screenplay.Diagnostics.Diagnostic> _diagnostics = [];
    protected ScreenplaySceneVisitor _visitor = null!;

    /// <summary>
    /// Compiles and translates a document.
    /// </summary>
    /// <param name="source">The Screenplay source.</param>
    protected void Translate(string source)
    {
        var compiled = new ScreenplayCompiler().Compile(source);
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}")));
        _diagnostics = [.. compiled.Diagnostics];
        _visitor = new ScreenplaySceneVisitor();
        _application = _visitor.Visit(compiled.Value!);
    }

    /// <summary>
    /// Translates a document the Screenplay compiler already rejects. The syntax tree is still complete, and Scene's own rules
    /// must reach the same verdict on it - the translation is defence in depth for a host that skips the compiler's checks.
    /// </summary>
    /// <param name="source">The Screenplay source.</param>
    protected void TranslateRejected(string source)
    {
        var compiled = new ScreenplayCompiler().Compile(source);
        _diagnostics = [.. compiled.Diagnostics];
        _visitor = new ScreenplaySceneVisitor();
        _application = _visitor.Visit(compiled.Value ?? throw new InvalidOperationException("The compiler returned no syntax tree."));
    }
}
