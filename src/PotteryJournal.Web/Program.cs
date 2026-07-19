using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Options;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;
using PotteryJournal.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
    options.Conventions.AllowAnonymousToPage("/Admin/AccessDenied");
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PotteryJournal")));

builder.Services.Configure<UploadsOptions>(builder.Configuration.GetSection("Uploads"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

builder.Services.AddScoped<IPieceHandler, PieceHandler>();
builder.Services.AddScoped<IReferenceDataHandler, ReferenceDataHandler>();
builder.Services.AddScoped<IEventsHandler, EventsHandler>();
builder.Services.AddScoped<IAllowedAdminsHandler, AllowedAdminsHandler>();
builder.Services.AddScoped<IAdminSettingsHandler, AdminSettingsHandler>();
builder.Services.AddScoped<IClassesHandler, ClassesHandler>();
builder.Services.AddScoped<IContactHandler, ContactHandler>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddSingleton<IIcsGenerator, IcsGenerator>();
builder.Services.AddSingleton<IRecurrenceExpander, RecurrenceExpander>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IPasswordHasher<AllowedAdmin>, PasswordHasher<AllowedAdmin>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/AccessDenied";
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(RateLimiterPolicies.DataEndpoints, limiterOptions =>
    {
        limiterOptions.PermitLimit = 120;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
    // Blunts password-guessing against /admin/login now that Google is no longer fronting sign-in.
    options.AddFixedWindowLimiter(RateLimiterPolicies.LoginAttempts, limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
    // Public, unauthenticated POST that writes to the database -- same treatment as LoginAttempts.
    options.AddFixedWindowLimiter(RateLimiterPolicies.ClassBooking, limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
    // Public, unauthenticated POST that sends email -- same treatment as ClassBooking.
    options.AddFixedWindowLimiter(RateLimiterPolicies.ContactForm, limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

using (IServiceScope startupScope = app.Services.CreateScope())
{
    AppDbContext dbContext = startupScope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsRelational())
    {
        // The in-memory provider used by integration tests doesn't support migrations.
        await dbContext.Database.MigrateAsync();
    }

    string? bootstrapAdminEmail = builder.Configuration["POTTERYJOURNAL_BOOTSTRAP_ADMIN_EMAIL"];
    string? bootstrapAdminPassword = builder.Configuration["POTTERYJOURNAL_BOOTSTRAP_ADMIN_PASSWORD"];
    if (string.IsNullOrWhiteSpace(bootstrapAdminEmail) || string.IsNullOrWhiteSpace(bootstrapAdminPassword))
    {
        app.Logger.LogWarning(
            "POTTERYJOURNAL_BOOTSTRAP_ADMIN_EMAIL and/or POTTERYJOURNAL_BOOTSTRAP_ADMIN_PASSWORD are not set -- if the AllowedAdmins list is empty, no one will be able to sign in.");
    }
    else
    {
        IAllowedAdminsHandler allowedAdminsHandler = startupScope.ServiceProvider.GetRequiredService<IAllowedAdminsHandler>();
        await allowedAdminsHandler.EnsureBootstrapAdminAsync(bootstrapAdminEmail, bootstrapAdminPassword);
    }

    IReferenceDataHandler referenceDataHandler = startupScope.ServiceProvider.GetRequiredService<IReferenceDataHandler>();
    await referenceDataHandler.EnsureSeedClassTypesAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

string uploadsRootPath = Path.Combine(app.Environment.ContentRootPath, app.Configuration["Uploads:RootPath"] ?? "uploads");
Directory.CreateDirectory(uploadsRootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRootPath),
    RequestPath = "/uploads",
    OnPrepareResponse = context =>
    {
        // Uploaded file names are unique per upload and never reused, so cache aggressively --
        // matches the original static site's nginx.conf treatment of piece photos.
        context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    },
});

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Plain JSON endpoints backing this site's own pages -- not a public API. They replace the
// old static site's checked-in pieces.json with server-generated data from Postgres.
app.MapGet("/pottery-journal/data", async (string? category, IPieceHandler pieceHandler) =>
{
    DataHandlerResponse<List<PieceDetailModel>> response = await pieceHandler.GetAllDetailsAsync(category);
    return JsonResult(response.Data ?? new List<PieceDetailModel>());
}).RequireRateLimiting(RateLimiterPolicies.DataEndpoints);

app.MapGet("/gallery/data", async (IPieceHandler pieceHandler) =>
{
    DataHandlerResponse<List<GalleryPieceModel>> response = await pieceHandler.GetGalleryPiecesAsync();
    return JsonResult(response.Data ?? new List<GalleryPieceModel>());
}).RequireRateLimiting(RateLimiterPolicies.DataEndpoints);

app.MapGet("/events/data", async (IEventsHandler eventsHandler) =>
{
    DataHandlerResponse<List<EventModel>> response = await eventsHandler.GetUpcomingAsync();
    return JsonResult(response.Data ?? new List<EventModel>());
}).RequireRateLimiting(RateLimiterPolicies.DataEndpoints);

app.MapGet("/events/data/all", async (IEventsHandler eventsHandler) =>
{
    DataHandlerResponse<List<EventModel>> response = await eventsHandler.GetOccurrencesAsync();
    return JsonResult(response.Data ?? new List<EventModel>());
}).RequireRateLimiting(RateLimiterPolicies.DataEndpoints);

app.MapGet("/classes/data", async (IClassesHandler classesHandler) =>
{
    // A fixed 90-day public booking window -- generous for planning ahead without ever needing to
    // expand indefinite recurrence unboundedly.
    DateTimeOffset now = DateTimeOffset.UtcNow;
    DataHandlerResponse<List<ClassSlotModel>> response = await classesHandler.GetAvailableSlotsAsync(now, now.AddDays(90));
    return JsonResult(response.Data ?? new List<ClassSlotModel>());
}).RequireRateLimiting(RateLimiterPolicies.DataEndpoints);

app.MapGet("/events/{id:guid}/ics", async (Guid id, IEventsHandler eventsHandler, IIcsGenerator icsGenerator) =>
{
    DataHandlerResponse<EventModel> eventResponse = await eventsHandler.GetByIdAsync(id);
    if (!eventResponse.IsSuccess || eventResponse.Data is null)
    {
        return Results.NotFound();
    }

    DataHandlerResponse<byte[]> icsResponse = icsGenerator.GenerateEventIcs(eventResponse.Data);
    if (!icsResponse.IsSuccess || icsResponse.Data is null)
    {
        return Results.Problem("Could not generate the calendar file.");
    }

    return Results.File(icsResponse.Data, "text/calendar", $"{eventResponse.Data.Title}.ics");
}).RequireRateLimiting(RateLimiterPolicies.DataEndpoints);

app.Run();

// Serializes to a JSON string up front rather than using Results.Json's streaming PipeWriter
// path, which is incompatible with the TestServer response pipe used by WebApplicationFactory
// integration tests. Payloads here are small, so the perf difference is immaterial.
static IResult JsonResult<T>(T data)
{
    JsonSerializerOptions options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    return Results.Content(JsonSerializer.Serialize(data, options), "application/json");
}

// Exposes the top-level statements' implicit Program class to PotteryJournal.Web.Tests for
// WebApplicationFactory<Program>.
public partial class Program
{
}
