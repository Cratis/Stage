// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_StageStaticFileOptions;

public class when_serving_an_asset : Specification
{
    DefaultHttpContext _context = null!;

    void Establish()
    {
        _context = new DefaultHttpContext();
        _context.Request.Path = "/assets/app.js";
    }

    void Because() => StageStaticFileOptions.Create().OnPrepareResponse(new StaticFileResponseContext(_context, Substitute.For<IFileInfo>()));

    [Fact] void should_allow_immutable_caching() => _context.Response.Headers.CacheControl.ToString().ShouldEqual("public,max-age=31536000,immutable");
}
