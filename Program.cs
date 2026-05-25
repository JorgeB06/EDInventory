using EDInventory.Data;
using EDInventory.Models.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

// ─────────────────────────────────────────────────────────────────
// EDInventory — Punto de entrada de la aplicación ASP.NET Core 8
// Stack: MVC + Pomelo EF Core + BCrypt.Net-Next + ClosedXML
// Base de datos: MySQL 8 (esquema DB_EInventory)
// Autenticación: Cookie "CookieAuth" con expiración deslizante de 8 h
// ─────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ── MySQL DbContext (Pomelo) ──────────────────────────────────────
// La cadena de conexión se lee de appsettings.json ("DefaultConnection")
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ── Data Protection ───────────────────────────────────────────────
// Persiste las claves de protección en disco para que las cookies antiforgery
// no se invaliden al reiniciar el servidor en modo desarrollo.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")))
    .SetApplicationName("EDInventory");

// ── Autenticación por cookie ──────────────────────────────────────
// Expiración de 8 horas con renovación deslizante.
// Redirige a /Auth/Login si no autenticado y a /Auth/AccessDenied si sin permisos.
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Ruta por defecto: Auth/Login (requiere autenticación para el resto)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

// ── Seed: usuario administrador inicial ───────────────────────────
// Si la base de datos no tiene ningún usuario, crea:
//   - Rol "Administrador"
//   - Tipo de documento "Cédula"
//   - Empleado "Admin Sistema"
//   - Usuario "admin" con contraseña "Admin123" (hash BCrypt)
// Debe cambiarse la contraseña después del primer acceso.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any())
    {
        // Rol de sistema
        var rol = new UserRole { UroleDesc = "Administrador", Active = true };
        db.UserRoles.Add(rol);

        // Tipo de documento empleado
        var docType = new DocumentTypeEmployee { DoctypeDesEmp = "Cedula", Active = true };
        db.DocumentTypeEmployees.Add(docType);
        await db.SaveChangesAsync();

        // Empleado
        var emp = new Employee
        {
            EmpName = "Admin",
            EmpSurname = "Sistema",
            DoctypeCodeEmp = docType.DoctypeCodeEmp,
            Active = true
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        // Usuario admin / Admin123
        db.Users.Add(new User
        {
            UserLogin = "admin",
            UserPassword = BCrypt.Net.BCrypt.HashPassword("Admin123"),
            EmpCode = emp.EmpCode,
            UroleCode = rol.UroleCode,
            Active = true
        });
        await db.SaveChangesAsync();

        Console.WriteLine("=== Usuario inicial creado: admin / Admin123 ===");
    }
}

app.Run();
