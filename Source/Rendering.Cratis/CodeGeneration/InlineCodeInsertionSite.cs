// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

internal readonly record struct InlineCodeInsertionSite(int Indent = 0, bool Verbatim = false, InlineSourceLine? Source = null);
