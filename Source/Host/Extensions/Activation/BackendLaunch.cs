// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Extensions.Packages;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// What a runtime is asked to start.
/// </summary>
/// <param name="System">The name of the system.</param>
/// <param name="Package">The package whose backend it is.</param>
/// <param name="Plan">How the backend runs.</param>
/// <param name="Address">The address the backend must listen on, which Stage connects to.</param>
public sealed record BackendLaunch(string System, LoadedExtensionPackage Package, ExecutionPlan Plan, Uri Address);
