// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Specifications;

internal static class Program
{
    static Task<int> Main(string[] args) => Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args);
}
#endif
