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

    public IdempotencyKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.Request.Headers.TryGetValue("Idempotency-Key", out var key))
        {
            var idempotencyKey = key.FirstOrDefault();
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var entry = await db.IdempotencyKeys.FindAsync(idempotencyKey);
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
                db.IdempotencyKeys.Add(newEntry);
                await db.SaveChangesAsync();
                await context.Response.WriteAsync(responseBody);
                return;
            }
        }
        await _next(context);
    }
}

// Great question! Let me explain** Dependency Injection(DI) in Middleware** clearly:

// ---

// ## 🔑 **What is Middleware DI?**

// ** Middleware** is code that runs in the request/response pipeline.In ASP.NET Core, middleware needs dependencies (like `DbContext`, services, etc.) through** Dependency Injection**.

// ---

// ## 📦 **Service Lifetimes in ASP.NET Core:**

// There are 3 types of service lifetimes:

// ### 1. **Singleton** 🌍
// - Created** once** for the entire application
// - Lives for the whole app lifetime
// - Example: Configuration, Logging

// ### 2. **Scoped** 🔄
// - Created** once per HTTP request**
// - Disposed after the request completes
// - Example: `DbContext`, `AppDbContext`

// ### 3. **Transient** ⚡
// - Created** every time** it's requested
// - Short-lived
// - Example: Lightweight utilities

// ---

// ## ⚠️ **The Middleware Problem:**

// ** Middleware itself is Singleton** (created once at app startup), but it often needs ** Scoped services** (like DbContext).

// ### ❌ **WRONG WAY - Constructor Injection:**

// ```csharp
// public class IdempotencyKeyMiddleware
// {
//     private readonly RequestDelegate _next;
//     private readonly AppDbContext _db;  // ❌ PROBLEM!

//     // Constructor runs ONCE at startup
//     public IdempotencyKeyMiddleware(RequestDelegate next, AppDbContext db)
//     {
//         _next = next;
//         _db = db;  // ❌ Captures a single DbContext forever!
//     }

//     public async Task InvokeAsync(HttpContext context)
//     {
//         // ❌ Using the same DbContext for ALL requests
//         var entry = await _db.IdempotencyKeys.FindAsync(key);
//     }
// }
// ```

// ** Why it fails:**
// - Middleware is created** once** (singleton)
// - `DbContext` is ** scoped** (per request)
// - You can't inject a scoped service into a singleton - it violates lifetime rules!
// - Even if allowed, you'd be using the **same DbContext** for all requests (thread-unsafe, data corruption risk)

// ---

// ## ✅ **CORRECT WAY - InvokeAsync Injection:**

// ```csharp
// public class IdempotencyKeyMiddleware
// {
//     private readonly RequestDelegate _next;
//     // No DbContext field! ✅

//     // Constructor runs ONCE - only inject singletons here
//     public IdempotencyKeyMiddleware(RequestDelegate next)
//     {
//         _next = next;
//     }

//     // InvokeAsync runs ONCE PER REQUEST - inject scoped services here
//     public async Task InvokeAsync(HttpContext context, AppDbContext db)
//     {
//         // ✅ Fresh DbContext for THIS request
//         var entry = await db.IdempotencyKeys.FindAsync(key);

//         await _next(context);
//     }
// }
// ```

// ** Why it works:**
// - `InvokeAsync` is called** once per HTTP request**
// - ASP.NET Core creates a ** new scope** for each request
// - It injects a** fresh `DbContext`** for each request
// - Each request gets its own isolated database context

// ---

// ## 📊 **Visual Comparison:**

// ### Constructor Injection (Wrong):
// ```
// App Startup
//     ↓
// Middleware Created(ONCE) ← AppDbContext injected HERE
//     ↓
// Request 1 → Uses SAME DbContext ❌
// Request 2 → Uses SAME DbContext ❌
// Request 3 → Uses SAME DbContext ❌
// ```

// ### InvokeAsync Injection (Correct):
// ```
// App Startup
//     ↓
// Middleware Created(ONCE)
//     ↓
// Request 1 → InvokeAsync called → NEW AppDbContext ✅
// Request 2 → InvokeAsync called → NEW AppDbContext ✅
// Request 3 → InvokeAsync called → NEW AppDbContext ✅
// ```

// ---

// ## 🎯 **The Rules:**

// | Service Type | Constructor | InvokeAsync |
// |--------------|-------------|-------------|
// | Singleton(Logger, Config) | ✅ Yes | ✅ Yes |
// | Scoped(DbContext) | ❌ NO | ✅ Yes |
// | Transient | ❌ Avoid | ✅ Yes |

// ---

// ## 💡 **Real-World Example:**

// ```csharp
// public class MyMiddleware
// {
//     private readonly RequestDelegate _next;
//     private readonly ILogger<MyMiddleware> _logger; // ✅ Singleton - OK in constructor

//     public MyMiddleware(RequestDelegate next, ILogger<MyMiddleware> logger)
//     {
//         _next = next;
//         _logger = logger; // ✅ Logger is singleton
//     }

//     // Parameters here are resolved PER REQUEST
//     public async Task InvokeAsync(
//         HttpContext context,
//         AppDbContext db,           // ✅ Scoped - fresh per request
//         IMyService service)        // ✅ Scoped - fresh per request
//     {
//         _logger.LogInformation("Processing request");

//         // Use db and service safely - they're fresh for this request
//         var data = await db.Users.FirstOrDefaultAsync();

//         await _next(context);
//     }
// }
// ```

// ---

// ## 🔧 **How ASP.NET Core Does It:**

// 1. ** App starts** → Middleware constructor called once
// 2. ** Request arrives** → ASP.NET creates a new ** scope**
// 3. ** InvokeAsync called** → ASP.NET resolves parameters from that scope
// 4. ** Request completes** → Scope disposed, DbContext disposed
// 5. ** Next request** → New scope, new DbContext

// ---

// ## 🎓 **Key Takeaway:**

// - ** Constructor**: Only for singleton dependencies(logger, config)
// - ** InvokeAsync**: For scoped dependencies(DbContext, request-specific services)

// This ensures ** thread safety** and ** proper resource management**! 