// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Names the slices whose generated code, and with it their Arc proxies, an application already holds under one of
/// its other domain folders.
/// </summary>
/// <param name="Domain">The domain sub-path those slices were generated under, such as Sales or Sales/Retail. Empty is the application root.</param>
/// <param name="Slices">The modules, features or slices generated under that domain.</param>
public sealed record UserInterfaceDomain(string Domain, PlanSelection Slices);
