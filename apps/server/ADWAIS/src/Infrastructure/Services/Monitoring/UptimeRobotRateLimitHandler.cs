// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Net;
using Adwais.Application.Common.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// A delegating handler that implements client-side rate limiting for the UptimeRobot API.
/// It monitors rate limit headers and status codes to delay requests when necessary.
/// </summary>
public class UptimeRobotRateLimitHandler(IMemoryCache cache) : DelegatingHandler
{
    private const string RateLimitKey = GlobalCacheKeys.UptimeRobotRateLimit;
    private const string RemainingLimitKey = "UptimeRobotRemainingLimit";
    private const string RateLimitResetKey = "UptimeRobotRateLimitReset";
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    /// <summary>
    /// Sends an HTTP request, potentially delaying it if a rate limit is active.
    /// Updates the rate limit state based on response headers.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Get)
        {
            if (cache.TryGetValue<int>(RemainingLimitKey, out var remaining) &&
                cache.TryGetValue<DateTimeOffset>(RateLimitResetKey, out var resetTime))
            {
                var now = DateTimeOffset.UtcNow;
                if (remaining <= 10 && resetTime > now)
                {
                    var delay = resetTime - now;
                    var response429 = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent("Rate limit budget preserved for edits."),
                        ReasonPhrase = "Rate Limit Budget Preserved"
                    };
                    response429.Headers.Add("Retry-After", ((int)Math.Max(1, delay.TotalSeconds)).ToString());
                    return response429;
                }
            }
        }

        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue<DateTimeOffset>(RateLimitKey, out var resetTime))
            {
                var delay = resetTime - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
        finally
        {
            Semaphore.Release();
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingVals) &&
            int.TryParse(remainingVals.FirstOrDefault(), out var remainingCount))
        {
            cache.Set(RemainingLimitKey, remainingCount);

            if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetVals) &&
                long.TryParse(resetVals.FirstOrDefault(), out var resetEpoch))
            {
                var resetDate = DateTimeOffset.FromUnixTimeSeconds(resetEpoch);
                cache.Set(RateLimitResetKey, resetDate);

                if (remainingCount <= 2 && resetDate > DateTimeOffset.UtcNow)
                {
                    cache.Set(RateLimitKey, resetDate, resetDate);
                }
            }
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var delay = TimeSpan.FromSeconds(10); // Default fallback
            if (response.Headers.TryGetValues("Retry-After", out var retryVals) &&
                int.TryParse(retryVals.FirstOrDefault(), out var retrySeconds))
            {
                delay = TimeSpan.FromSeconds(retrySeconds);
            }
            else if (response.Headers.RetryAfter?.Delta.HasValue == true)
            {
                delay = response.Headers.RetryAfter.Delta.Value;
            }

            var resetDate = DateTimeOffset.UtcNow.Add(delay).AddSeconds(1);
            cache.Set(RateLimitKey, resetDate, resetDate);
            cache.Set(RemainingLimitKey, 0);
            cache.Set(RateLimitResetKey, resetDate);

            // Wait out the delay and retry the request
            await Task.Delay(delay.Add(TimeSpan.FromSeconds(1)), cancellationToken);
            using var clonedRequest = await CloneHttpRequestMessageAsync(request);
            return await base.SendAsync(clonedRequest, cancellationToken);
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri)
        {
            Version = req.Version
        };

        foreach (var header in req.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in req.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        if (req.Content != null)
        {
            var ms = new System.IO.MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            
            var streamContent = new StreamContent(ms);
            foreach (var header in req.Content.Headers)
            {
                streamContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            clone.Content = streamContent;
        }

        return clone;
    }
}


