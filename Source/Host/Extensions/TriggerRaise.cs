// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// A trigger an extension raises.
/// </summary>
/// <param name="System">The name of the system the trigger belongs to.</param>
/// <param name="Trigger">The name of the trigger.</param>
/// <param name="Tenant">The tenant it is raised for.</param>
/// <param name="DeduplicationKey">The key that makes raising it again find the first raise.</param>
/// <param name="PayloadJson">What the trigger carries, as JSON.</param>
/// <param name="Causation">What caused it, such as the invocation that raised it.</param>
/// <param name="Correlation">What it is about. Runs that wait for the trigger about the same thing resume; empty resumes none.</param>
public sealed record TriggerRaise(string System, string Trigger, string Tenant, string DeduplicationKey, string PayloadJson, string Causation, string Correlation = "");
