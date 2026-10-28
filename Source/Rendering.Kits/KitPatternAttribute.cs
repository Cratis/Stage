// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

[AttributeUsage(AttributeTargets.Property)]
sealed class KitPatternAttribute(string pattern) : Attribute
{
    public string Pattern { get; } = pattern;
}
