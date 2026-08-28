using System.Net;
using FluentAssertions;
using Greenlens.Application.Common.Options;
using Greenlens.Infrastructure.Meta;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Greenlens.Application.UnitTests;

public sealed class MetaGraphPagePublisherTests
{
    [Fact]
    public async Task PublishPhotoPostAsync_Success_ReturnsPostId()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"122189724434922710","post_id":"642977455573500_122189724434922710"}""")
            });

        var publisher = CreatePublisher(handler, new MetaPageOptions
        {
            PageId = "642977455573500",
            PageAccessToken = "page-token",
            AutoPostEnabled = true
        });

        var result = await publisher.PublishPhotoPostAsync(
            "Hello",
            "https://cdn.test/report.jpg",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("642977455573500_122189724434922710");
        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/v21.0/642977455573500/photos");
    }

    [Fact]
    public async Task PublishPhotoPostAsync_GraphError_ReturnsFailure()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"error":{"message":"Invalid OAuth access token","type":"OAuthException","code":190}}""")
            });

        var publisher = CreatePublisher(handler, new MetaPageOptions
        {
            PageId = "642977455573500",
            PageAccessToken = "bad-token"
        });

        var result = await publisher.PublishPhotoPostAsync(
            "Hello",
            "https://cdn.test/report.jpg",
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("META_PAGE_PUBLISH_FAILED");
        result.Error.Message.Should().Contain("Invalid OAuth access token");
    }

    [Fact]
    public async Task PublishPhotoPostAsync_NotConfigured_ReturnsFailure()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var publisher = CreatePublisher(handler, new MetaPageOptions { AutoPostEnabled = true });

        var result = await publisher.PublishPhotoPostAsync(
            "Hello",
            "https://cdn.test/report.jpg",
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("META_PAGE_NOT_CONFIGURED");
        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishPhotoPostAsync_MissingImageUrl_ReturnsFailure()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var publisher = CreatePublisher(handler, new MetaPageOptions
        {
            PageId = "642977455573500",
            PageAccessToken = "page-token"
        });

        var result = await publisher.PublishPhotoPostAsync("Hello", "", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("META_PAGE_SHARE_IMAGE_REQUIRED");
        handler.RequestCount.Should().Be(0);
    }

    private static MetaGraphPagePublisher CreatePublisher(StubHttpMessageHandler handler, MetaPageOptions options)
    {
        var factory = new StubHttpClientFactory(handler);
        return new MetaGraphPagePublisher(factory, Options.Create(options), NullLogger<MetaGraphPagePublisher>.Instance);
    }

    private sealed class StubHttpClientFactory(StubHttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://graph.facebook.com/")
        };
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }
}
