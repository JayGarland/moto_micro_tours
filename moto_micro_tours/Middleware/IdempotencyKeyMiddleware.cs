using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Middleware;

/// <summary>
/// Response caching for retried requests that carry an "Idempotency-Key" header.
///
/// Guarantees and limits (course-project scope):
/// - Runs AFTER authentication/authorization, so a cache hit can never bypass
///   the current request's auth checks. Resource-based checks inside a
///   controller do not re-run on replay, but the cache is user-scoped, so a
///   replay only ever returns the caller's own earlier response.
/// - The stored key is a SHA-256 over (user, method, path, query, request body,
///   client key): the same client key from another user, on another endpoint,
///   or with a different body never hits this cache entry.
/// - Only authenticated non-GET requests are cached; anonymous requests and
///   safe methods pass straight through.
/// - Atomicity: a claim row (StatusCode = 0) is inserted BEFORE the endpoint
///   runs, so two concurrent first uses of the same key cannot both execute;
///   the loser gets 409 Conflict. 5xx responses are not cached, so failed
///   operations can be retried.
/// </summary>
public class IdempotencyKeyMiddleware
{
    private const string HeaderName = "Idempotency-Key";
    private const int InFlightStatusCode = 0; // claim marker, never a real HTTP status

    private readonly RequestDelegate _next;

    public IdempotencyKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        string? clientKey = null;
        if (context.Request.Headers.TryGetValue(HeaderName, out var keyValues))
        {
            clientKey = keyValues.FirstOrDefault();
        }

        var isCacheableMethod = context.Request.Method != HttpMethods.Get
                                && context.Request.Method != HttpMethods.Head
                                && context.Request.Method != HttpMethods.Options
                                && context.Request.Method != HttpMethods.Trace;

        if (string.IsNullOrEmpty(clientKey) || !isCacheableMethod || context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var cacheKey = await BuildCacheKeyAsync(context, context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value, clientKey);

        // Replay path: an existing completed entry for this user/endpoint/key
        var existing = await db.IdempotencyKeys.FindAsync(cacheKey);
        if (existing != null)
        {
            if (existing.StatusCode == InFlightStatusCode)
            {
                // Same key already executing in another request
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"An identical request with this Idempotency-Key is already in progress\"}");
                return;
            }

            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = existing.ContentType;
            await context.Response.WriteAsync(existing.ResponseBody);
            return;
        }

        // Claim path: insert first, so a concurrent request with the same key
        // loses on the primary key instead of double-executing the endpoint
        var claim = new IdempotencyKey
        {
            Key = cacheKey,
            ResponseBody = string.Empty,
            StatusCode = InFlightStatusCode,
            ContentType = "application/json",
            CreatedAt = DateTime.UtcNow
        };
        db.IdempotencyKeys.Add(claim);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A concurrent request claimed the same key between our lookup and insert
            db.ChangeTracker.Clear();
            var winner = await db.IdempotencyKeys.FindAsync(cacheKey);
            if (winner != null && winner.StatusCode != InFlightStatusCode)
            {
                // The other request already finished: replay its response
                context.Response.StatusCode = winner.StatusCode;
                context.Response.ContentType = winner.ContentType;
                await context.Response.WriteAsync(winner.ResponseBody);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"An identical request with this Idempotency-Key is already in progress\"}");
            }
            return;
        }

        // Read the request body (already consumed above for hashing) so the endpoint can bind it
        context.Request.EnableBuffering();
        context.Request.Body.Seek(0, SeekOrigin.Begin);

        // Execute and buffer the response
        var originalBody = context.Response.Body;
        await using var memStream = new MemoryStream();
        context.Response.Body = memStream;
        try
        {
            await _next(context);
        }
        catch
        {
            // Release the claim so the client can retry the failed operation
            context.Response.Body = originalBody;
            db.IdempotencyKeys.Remove(claim);
            await db.SaveChangesAsync();
            throw;
        }

        memStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(memStream).ReadToEndAsync();
        context.Response.Body = originalBody;

        // Only cache successful client-side outcomes; 5xx stays retryable
        if (context.Response.StatusCode < 500 && !context.Response.HasStarted)
        {
            claim.StatusCode = context.Response.StatusCode;
            claim.ContentType = context.Response.ContentType ?? "application/json";
            claim.ResponseBody = responseBody;
            await db.SaveChangesAsync();
        }
        else
        {
            db.IdempotencyKeys.Remove(claim);
            await db.SaveChangesAsync();
        }

        await context.Response.WriteAsync(responseBody);
    }

    /// <summary>
    /// Composite cache key: user + method + path + query + body + client key.
    /// Hashed because the raw composite can exceed the key column length.
    /// Public so tests can seed in-flight claim rows deterministically.
    /// </summary>
    public static async Task<string> BuildCacheKeyAsync(HttpContext context, string userId, string clientKey)
    {
        context.Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(
                   context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
                   bufferSize: 1024, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }
        context.Request.Body.Seek(0, SeekOrigin.Begin);

        var composite = string.Join('\n',
            userId,
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty,
            context.Request.QueryString.Value ?? string.Empty,
            body,
            clientKey);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(composite));
        return Convert.ToHexString(hash);
    }
}
