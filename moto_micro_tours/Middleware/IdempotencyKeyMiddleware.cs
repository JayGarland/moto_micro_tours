using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text.Json;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Middleware;

public class IdempotencyKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppDbContext _db;

    public IdempotencyKeyMiddleware(RequestDelegate next, AppDbContext db)
    {
        _next = next;
        _db = db;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Idempotency-Key", out var key))
        {
            var idempotencyKey = key.FirstOrDefault();
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var entry = await _db.IdempotencyKeys.FindAsync(idempotencyKey);
                if (entry != null)
                {
                    // Return cached response
                    context.Response.StatusCode = entry.StatusCode;
                    context.Response.ContentType = entry.ContentType;
                    await context.Response.WriteAsync(entry.ResponseBody);
                    return;
                }
                // Buffer response
                var originalBody = context.Response.Body;
                using var memStream = new System.IO.MemoryStream();
                context.Response.Body = memStream;
                await _next(context);
                memStream.Seek(0, System.IO.SeekOrigin.Begin);
                var responseBody = new System.IO.StreamReader(memStream).ReadToEnd();
                context.Response.Body = originalBody;
                // Persist response
                var newEntry = new IdempotencyKey
                {
                    Key = idempotencyKey,
                    ResponseBody = responseBody,
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType ?? "application/json",
                    CreatedAt = DateTime.UtcNow
                };
                _db.IdempotencyKeys.Add(newEntry);
                await _db.SaveChangesAsync();
                await context.Response.WriteAsync(responseBody);
                return;
            }
        }
        await _next(context);
    }
}
