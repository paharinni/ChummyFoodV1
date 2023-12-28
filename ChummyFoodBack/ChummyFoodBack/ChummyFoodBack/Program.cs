using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using ChummyFoodBack.Exceptions;
using ChummyFoodBack.Factories;
using ChummyFoodBack.Feature.IdentityManagement;
using ChummyFoodBack.Feature.Notification;
using ChummyFoodBack.Feature.Payment;
using ChummyFoodBack.Feature.Payment.Interfaces;
using ChummyFoodBack.Feature.RetailManagement.Products;
using ChummyFoodBack.Feature.Statistics.Orders;
using ChummyFoodBack.Feature.Statistics.Users;
using ChummyFoodBack.Feature.VoucherManagement;
using ChummyFoodBack.Files;
using ChummyFoodBack.Interactions;
using ChummyFoodBack.Interactions.Intefaces;
using ChummyFoodBack.Options;
using ChummyFoodBack.Persistance;
using ChummyFoodBack.Persistance.DAO;
using FansEcomerseSite.Feature.Payment.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

const string SubdirName = "Photos";
string targetContentRoot = Path.Combine(Directory.GetCurrentDirectory(), SubdirName);
if (!Directory.Exists(targetContentRoot))
{
    Directory.CreateDirectory(targetContentRoot);
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = targetContentRoot,
});

var coinbaseSecurityApiClientFactory =
    (IServiceProvider serviceProvider, HttpClient httpClient) =>
    {
        var paymentOptions = serviceProvider
            .GetRequiredService<IOptions<PaymentOptions>>();
        CoinbaseHttpClientFactory.AddCoinbaseSecurity(paymentOptions.Value.ApiKey, httpClient);
    };


builder.Services.AddTransient<IssueCoinbasePayment>();
builder.Services.AddHttpClient<IssueCoinbasePayment>(
    coinbaseSecurityApiClientFactory);

builder.Services.AddSingleton<IPasswordGenerator, PasswordGenerator>();
builder.Services.AddControllers(opts =>
{
    opts.CacheProfiles.Add("DefaultCache", new CacheProfile
    {
        Duration = 60,
        Location = ResponseCacheLocation.Any,
        
    });
}).AddJsonOptions(opts =>
{
    var enumConverter = new JsonStringEnumConverter();
    opts.JsonSerializerOptions.Converters.Add(enumConverter);
    opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.Configure<SecurityOptions>(
    builder.Configuration
        .GetSection(SecurityOptions.Security));
builder.Services.Configure<AdminUserOptions>(
    builder.Configuration.GetSection(AdminUserOptions.AdminUser));
builder.Services
    .Configure<MailOptions>(builder.Configuration.GetSection(MailOptions.Mail));
builder.Services
    .Configure<PaymentOptions>
        (builder.Configuration.GetSection(PaymentOptions.Payment));

builder.Services.AddScoped<IBalanceService, BalanceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentAction, IssueCoinbasePayment>();
builder.Services.AddScoped<IVoucherService, VoucherService>();

builder.Services.AddScoped<IUserStatisticsService, UserStatisticsService>();
builder.Services.AddScoped<IOrdersStatisticsService, OrdersStatisticsService>();

builder.Services.AddResponseCaching();
builder.Services.AddScoped<IUserNotificationService, UserNotificationService>();
builder.Services.AddScoped<IMailInteractionService,MailInteractionService>();
builder.Services.AddSingleton<ProductsEndpointGenerator>();
builder.Services.AddTransient<IIdentityService, IdentityService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    var secretKeyString = builder.Configuration
        .GetSection(SecurityOptions.Security)
        [nameof(SecurityOptions.SecretKey)];
    if(secretKeyString is null)
    {
        throw new ConfigurationException("Secret key should be passed");
    }
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = TokenOptions.Issuer,
        ValidAudience = TokenOptions.Audience,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKeyString!))
    };
});

builder.Services.AddDbContext<CommerceContext>((services, dbContext) =>
{
    string? targetConnectionString = builder.Configuration.GetConnectionString("Default");
    if (targetConnectionString == null)
    {
        throw new ConfigurationException($"Not passed connection string with name Default");
    }
    
    dbContext.UseMySql(targetConnectionString, ServerVersion.AutoDetect(targetConnectionString), options =>
    {
        options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});
builder.Services.AddSingleton<ImageFileManagement>();
builder.Services.AddCors(options =>
{
    string? allowedHosts = builder.Configuration.GetSection("Cors")["AllowedOrigin"];
    if (allowedHosts is null)
    {
        throw new ConfigurationException("Allowed hosts should be passed!");
    }
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

builder.Services
    .Configure<ImageUrlOptions>(builder.Configuration.GetSection(ImageUrlOptions.Image));
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<CommerceContext>();
    var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();


    ctx.Database.EnsureCreated();
    var adminConfig = builder.Configuration.GetSection(AdminUserOptions.AdminUser)
        .Get<AdminUserOptions>();

    if (adminConfig is null)
    {
        throw new ConfigurationException(
            "Admin configuration should be passed");
    }

    var modelFromOptions = new IdentityModel
    {
        Email = adminConfig.Email,
        Password = adminConfig.Password
    };
    var existedAdminIdentity =
        await ctx.Identities.Include(identity => identity.RoleDao)
            .FirstOrDefaultAsync(identity => identity.RoleDao.Name == "Admin");

    if (existedAdminIdentity is null)
    {
        var result = PasswordHasher.HashPassword(modelFromOptions.Password);
        await ctx.Identities.AddAsync(new IdentityDAO
        {
            Email = adminConfig.Email,
            PasswordHash = result.PasswordHash,
            PasswordSalt = result.PasswordSalt,
            RoleDao = new RoleDao
            {
                Name = "Admin"
            },
            Name = "test",
            Surname = "test",
            Age = 18,
            City = "test",
            Country = "test",
            
        });
        await ctx.SaveChangesAsync();
    }else if (!identityService.CheckIdentitiesEqual(modelFromOptions, existedAdminIdentity))
    {
        var hashedPassword = PasswordHasher.HashPassword(modelFromOptions.Password);
        existedAdminIdentity.Email = modelFromOptions.Email;
        existedAdminIdentity.PasswordHash = hashedPassword.PasswordHash;
        existedAdminIdentity.PasswordSalt = hashedPassword.PasswordSalt;
        await ctx.SaveChangesAsync();
    }

    if (!await ctx.Categories.AnyAsync(category => category.Id == -1))
    {
        await ctx.Categories.AddAsync(new CategoryDAO
        {
            Id = -1,
            Name = "All"
        }); 
        await ctx.SaveChangesAsync();
    };
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
