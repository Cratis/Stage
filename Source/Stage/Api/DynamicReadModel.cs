// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Api;

/// <summary>
/// Base type for the runtime read model types the engine emits per modeled read model. A distinct runtime type
/// per read model keeps query performers uniquely identifiable to Arc even though the read models have no
/// compile-time shape.
/// </summary>
public class DynamicReadModel;
