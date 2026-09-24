// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_StageStaticFileOptions;

public class when_serving_a_page : Specification
{
    DefaultHttpContext _context = null!;

    void Establish() => _context = new DefaultHttpContext();
    void Because() => StageStaticFileOptions.Create().OnPrepareResponse(new StaticFileResponseContext(_context, Substitute.For<IFileInfo>()));

    [Fact] void should_prevent_caching_between_sessions() => _context.Response.Headers.CacheControl.ToString().ShouldEqual("no-store");
}
