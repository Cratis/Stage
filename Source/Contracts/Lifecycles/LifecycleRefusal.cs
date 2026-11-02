// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Lifecycles;

/// <summary>
/// Names a state machine or workflow construct that Stage cannot execute or render yet, with the diagnostic that says why.
/// </summary>
/// <param name="Code">The <c>STAGE-LIFECYCLE-*</c> diagnostic code.</param>
/// <param name="Owner">The name of the state machine or workflow.</param>
/// <param name="Message">What is not supported, and what to do instead.</param>
public sealed record LifecycleRefusal(string Code, string Owner, string Message);
