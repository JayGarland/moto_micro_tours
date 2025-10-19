using System;

namespace moto_micro_tours.Domain.Entities;

public class IdempotencyKey
{
    // Middleware-level idempotency storage: map key -> cached HTTP response metadata
    // Use this to short-circuit repeated identical HTTP requests (e.g., payment calls)
    // by returning the previously stored response body/status instead of re-processing.
    // Example: IdempotencyKey.Key = "abc-123"; ResponseBody = "{...}";
    public string Key { get; set; } = null!;
    public string ResponseBody { get; set; } = null!;
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// Here's a clear explanation of how idempotency keys work in your project, and why there are two different tables:

// ---

// **1. What is an idempotency key?**
// - An idempotency key is a unique string sent by a client (usually in an HTTP header) to ensure that repeated requests (due to retries, network issues, etc.) only result in one action on the server.
// - Example: If a user tries to pay for a booking and the request is retried, the same idempotency key ensures only one payment is processed.

// ---

// **2. Why do you have two tables?**

// - **PaymentKey**
//   - Purpose: Tracks idempotency for payment actions, scoped to a specific booking.
//   - Fields: `Id`, `BookingId`, `IdempotencyKey`, `CreatedAt`
//   - Usage: When a payment is attempted for a booking, a `PaymentKey` row is created. If the same key is used again for that booking, the system returns the original payment result instead of processing a duplicate payment.

// - **IdempotencyKey**
//   - Purpose: General-purpose idempotency cache for any HTTP request, not just payments.
//   - Fields: `Key`, `ResponseBody`, `StatusCode`, `ContentType`, `CreatedAt`
//   - Usage: Middleware can store the full HTTP response for any request with an idempotency key. If the same request is retried, the cached response is returned immediately.

// ---

// **3. How does the flow work?**

// - **Payment flow (using PaymentKey):**
//   1. Client sends a payment request with an `Idempotency-Key` header.
//   2. Server checks if a `PaymentKey` exists for that booking and key.
//      - If yes: Return the previous payment result.
//      - If no: Process payment, create a new `PaymentKey` row, and return the result.

// - **General API flow (using IdempotencyKey):**
//   1. Client sends any request with an `Idempotency-Key` header.
//   2. Middleware checks if an `IdempotencyKey` row exists for that key.
//      - If yes: Return the cached HTTP response.
//      - If no: Process the request, store the response in `IdempotencyKey`, and return it.

// ---

// **4. Why consider merging?**
// - If you want a single source of truth for all idempotency keys (payments and other requests), you can merge these tables.
// - The merged table would store both the booking/payment context and the cached response, making it easier to manage and query idempotency across the whole app.


