using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using WebApplication8.Data;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ إعداد قاعدة البيانات
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("defaultConnection")));

// 2️⃣ إضافة Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3️⃣ إعداد Cookie Authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/api/user/login"; // مسار login
    options.AccessDeniedPath = "/api/user/forbidden"; // مسار forbidden
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4️⃣ Seed Roles + Admin + User
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // إنشاء Roles
    if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
        roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();

    if (!roleManager.RoleExistsAsync("User").GetAwaiter().GetResult())
        roleManager.CreateAsync(new IdentityRole("User")).GetAwaiter().GetResult();

    // إنشاء Admin
    var adminEmail = "admin@example.com";
    var adminUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        userManager.CreateAsync(adminUser, "Admin@123").GetAwaiter().GetResult();
        userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
    }

    // إنشاء User عادي
    var userEmail = "user@example.com";
    var normalUser = userManager.FindByEmailAsync(userEmail).GetAwaiter().GetResult();
    if (normalUser == null)
    {
        normalUser = new IdentityUser
        {
            UserName = userEmail,
            Email = userEmail,
            EmailConfirmed = true
        };

        userManager.CreateAsync(normalUser, "User@123").GetAwaiter().GetResult();
        userManager.AddToRoleAsync(normalUser, "User").GetAwaiter().GetResult();
    }
}

// 5️⃣ Middleware لمعالجة الأخطاء
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (errorFeature != null)
        {
            var ex = errorFeature.Error;
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception caught by middleware");

            await context.Response.WriteAsJsonAsync(new
            {
                status = 500,
                message = "Something went wrong, please try again later."
            });
        }
    });
});

// 6️⃣ Swagger
app.UseSwagger();
app.UseSwaggerUI();

// 7️⃣ HTTPS + Authentication + Authorization
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// 8️⃣ Map Controllers
app.MapControllers();

app.Run();
