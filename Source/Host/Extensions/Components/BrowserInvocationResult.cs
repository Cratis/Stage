// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// The result of a request to invoke from the browser.
/// </summary>
/// <param name="Status">The <see cref="BrowserInvocationStatus"/>.</param>
/// <param name="Identity">The invocation identity, empty when no invocation exists.</param>
/// <param name="Outcome">The outcome when settled: <c lang="text">returned</c>, a declared outcome or <c lang="text">failed</c>.</param>
/// <param name="Output">What the ability returned, as JSON, when settled.</param>
/// <param name="Reason">Why it was refused, or why the invocation failed.</param>
public sealed record BrowserInvocationResult(BrowserInvocationStatus Status, string Identity = "", string Outcome = "", string Output = "", string Reason = "");
