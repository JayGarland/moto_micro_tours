using moto_micro_tours;
using moto_micro_tours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// Authorization: register conversation policies and handlers
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanAccessPreBookingConversation", policy =>
        policy.Requirements.Add(new moto_micro_tours.Authorization.ConversationAccessRequirement()));
    options.AddPolicy("CanAccessBookingConversation", policy =>
        policy.Requirements.Add(new moto_micro_tours.Authorization.ConversationAccessRequirement()));
});

builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, moto_micro_tours.Authorization.PreBookingConversationHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, moto_micro_tours.Authorization.BookingConversationHandler>();

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

app.UseHttpsRedirection();

// Add Idempotency middleware early in the pipeline
app.UseMiddleware<moto_micro_tours.Middleware.IdempotencyKeyMiddleware>();

// TODO(STAGE F): When enabling real authentication, call app.UseAuthentication() here
// before app.UseAuthorization(), and add [Authorize] attributes to controllers/endpoints.
// Example:
// app.UseAuthentication();
// app.UseAuthorization();

app.UseAuthorization();

app.MapControllers();

// Seed the database with happy path data if empty
await moto_micro_tours.DbSeeder.SeedAsync(app.Services);

app.Run();
