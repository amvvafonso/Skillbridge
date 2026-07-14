using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Skillbridge.Auth;
using Skillbridge.Data;
using Skillbridge.Hubs;
using Skillbridge.Models.Client;
using Skillbridge.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IS3Api, IS3Api.S3Api>();
builder.Services.AddScoped<IProjectService, IProjectService.ProjectService>();
builder.Services.AddScoped<INotificationService, INotificationService.NotificationService>(); 
builder.Services.AddScoped<ISessionService, ISessionService.SessionService>();
builder.Services.AddScoped<IApiTokenService, IApiTokenService.ApiTokenService>();
builder.Services.AddScoped<IOrganizationMemberService, IOrganizationMemberService>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthentication>("ApiKey", null);

//Aceita o token CSRF via header HTTP
builder.Services.AddAntiforgery(options => {
    options.HeaderName = "RequestVerificationToken";
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SkillBridge API",
        Version = "v1",
        Description = "API para a plataforma Skillbridge"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insere o token no formato: Bearer {o-teu-token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Sintaxe nova, baseada em delegate — resolve o erro que tiveste antes
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SkillBridge API v1");
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapSwagger().RequireAuthorization();
app.MapStaticAssets();
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHub<CodeEditorHub>("/codeEditorHub");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Client}/{action=Index}/{id?}");

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();