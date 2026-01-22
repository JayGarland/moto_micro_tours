using moto_micro_tours;
using moto_micro_tours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add SignalR for real-time messaging
builder.Services.AddSignalR();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// Configure Identity with Cookie authentication
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // For demo purposes, relax password requirements
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;

    // Return 401 instead of redirecting to login page (for API)
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Authorization: register conversation and booking policies and handlers
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanAccessPreBookingConversation", policy =>
        policy.Requirements.Add(new moto_micro_tours.Authorization.ConversationAccessRequirement()));
    options.AddPolicy("CanAccessBookingConversation", policy =>
        policy.Requirements.Add(new moto_micro_tours.Authorization.ConversationAccessRequirement()));
    options.AddPolicy("CanAccessBookingDetails", policy =>
        policy.Requirements.Add(new moto_micro_tours.Authorization.BookingAccessRequirement()));
});

// Phase 1: Register authorization helpers
builder.Services.AddScoped<moto_micro_tours.Services.IAuthorizationHelpers, moto_micro_tours.Services.AuthorizationHelpers>();

builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, moto_micro_tours.Authorization.PreBookingConversationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, moto_micro_tours.Authorization.BookingConversationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, moto_micro_tours.Authorization.BookingAccessHandler>();

// Register application services
builder.Services.AddScoped<moto_micro_tours.Services.IPaymentService, moto_micro_tours.Services.PaymentService>();
builder.Services.AddScoped<moto_micro_tours.Services.IMessageService, moto_micro_tours.Services.MessageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Only use HTTPS redirection in production
    app.UseHttpsRedirection();
}

// Enable CORS - must be before authentication/authorization
app.UseCors("AllowAngularDev");

// Add Idempotency middleware early in the pipeline
app.UseMiddleware<moto_micro_tours.Middleware.IdempotencyKeyMiddleware>();

// Enable authentication and authorization (Stage F)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<moto_micro_tours.Hubs.BookingChatHub>("/hubs/booking-chat");
app.MapHub<moto_micro_tours.Hubs.PreBookingChatHub>("/hubs/prebooking-chat");

// Seed the database with happy path data if empty
await moto_micro_tours.DbSeeder.SeedAsync(app.Services);

app.Run();
