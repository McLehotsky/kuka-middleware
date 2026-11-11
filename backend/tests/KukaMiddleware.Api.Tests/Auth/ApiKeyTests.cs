using KukaMiddleware.Api.Auth;
using KukaMiddleware.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace KukaMiddleware.Api.Tests.Auth;

public class ApiKeyTests
{
    private const string Key = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private readonly IApiClientStore _store = Substitute.For<IApiClientStore>();
    private readonly CurrentApiClient _current = new();

    private static DefaultHttpContext Context(string path, string? key = null)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider(),
        };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        if (key is not null) context.Request.Headers[ApiKeyMiddleware.HeaderName] = key;
        return context;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong")]
    public async Task Missing_or_unknown_key_is_401_without_calling_endpoint(string? key)
    {
        var called = false;
        var middleware = new ApiKeyMiddleware(_ => { called = true; return Task.CompletedTask; }, NullLogger<ApiKeyMiddleware>.Instance);
        var context = Context("/api/missions", key);

        await middleware.InvokeAsync(context, _store, _current);

        Assert.Equal(401, context.Response.StatusCode);
        Assert.False(called);
        Assert.Null(_current.Client);
    }

    [Fact]
    public async Task Inactive_key_is_401()
    {
        _store.FindByKeyHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ApiClient { Id = 7, Name = "inactive", KeyHash = "hash", IsActive = false });
        var context = Context("/api/robots", Key);
        var middleware = new ApiKeyMiddleware(_ => throw new InvalidOperationException(), NullLogger<ApiKeyMiddleware>.Instance);

        await middleware.InvokeAsync(context, _store, _current);

        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task Valid_key_sets_the_client_and_only_looks_up_its_hash()
    {
        var client = new ApiClient { Id = 7, Name = "external-system", KeyHash = ApiKeySettings.Hash(Key), IsActive = true };
        _store.FindByKeyHashAsync(client.KeyHash, Arg.Any<CancellationToken>()).Returns(client);
        var called = false;
        var middleware = new ApiKeyMiddleware(_ => { called = true; return Task.CompletedTask; }, NullLogger<ApiKeyMiddleware>.Instance);

        await middleware.InvokeAsync(Context("/api/missions", Key), _store, _current);

        Assert.True(called);
        Assert.Same(client, _current.Client);
        await _store.Received(1).FindByKeyHashAsync(client.KeyHash, Arg.Any<CancellationToken>());
        await _store.DidNotReceive().FindByKeyHashAsync(Key, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/api/health/")]
    public async Task Health_is_public(string path)
    {
        var called = false;
        var middleware = new ApiKeyMiddleware(_ => { called = true; return Task.CompletedTask; }, NullLogger<ApiKeyMiddleware>.Instance);

        await middleware.InvokeAsync(Context(path), _store, _current);

        Assert.True(called);
        await _store.DidNotReceiveWithAnyArgs().FindByKeyHashAsync(default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Health_prefix_does_not_bypass_authentication()
    {
        var context = Context("/api/health/private");
        var middleware = new ApiKeyMiddleware(_ => throw new InvalidOperationException(), NullLogger<ApiKeyMiddleware>.Instance);

        await middleware.InvokeAsync(context, _store, _current);

        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task Multiple_key_headers_are_rejected()
    {
        var context = Context("/api/missions", Key);
        context.Request.Headers.Append(ApiKeyMiddleware.HeaderName, Key);
        var middleware = new ApiKeyMiddleware(_ => throw new InvalidOperationException(), NullLogger<ApiKeyMiddleware>.Instance);

        await middleware.InvokeAsync(context, _store, _current);

        Assert.Equal(401, context.Response.StatusCode);
        await _store.DidNotReceiveWithAnyArgs().FindByKeyHashAsync(default!, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("change-me")]
    [InlineData("bbbb")]
    public void Invalid_configured_key_is_rejected_without_disclosing_it(string? key)
    {
        var error = Assert.Throws<InvalidOperationException>(() => new ApiKeySettings(key!, Key));
        Assert.DoesNotContain(Key, error.Message);
        if (!string.IsNullOrEmpty(key)) Assert.DoesNotContain(key, error.Message);
    }

    [Fact]
    public void Duplicate_configured_keys_are_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => new ApiKeySettings(Key, Key));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("change-me")]
    [InlineData("bbbb")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    public void Invalid_external_key_is_rejected_without_disclosing_it(string? key)
    {
        var error = Assert.Throws<InvalidOperationException>(() => new ApiKeySettings(Key, key!));
        Assert.DoesNotContain(Key, error.Message);
        if (!string.IsNullOrEmpty(key)) Assert.DoesNotContain(key, error.Message);
    }

    [Fact]
    public async Task Rejection_is_logged_with_correlation_but_without_the_key()
    {
        var logger = new TestLogger<ApiKeyMiddleware>();
        var context = Context("/api/missions", Key);
        context.TraceIdentifier = "rejected-request";
        var middleware = new ApiKeyMiddleware(_ => throw new InvalidOperationException(), logger);

        await middleware.InvokeAsync(context, _store, _current);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, entry.Level);
        Assert.Contains(entry.Fields, field => field.Key == "CorrelationId" && Equals(field.Value, "rejected-request"));
        Assert.Contains(entry.Fields, field => field.Key == "Path" && field.Value!.ToString() == "/api/missions");
        Assert.DoesNotContain(Key, entry.Message);
        Assert.DoesNotContain(entry.Fields, field => field.Value?.ToString()?.Contains(Key) == true);
    }
}
