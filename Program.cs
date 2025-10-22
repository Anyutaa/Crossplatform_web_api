using Crossplatform_2_smirnova.Data;
using Crossplatform_2_smirnova.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<BookingService>();

// Add services to the container.
builder.Services.AddControllers();

// Подключение EF Core (SQLite)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Booking System API Smirnova",
        Description = "API системы бронирования помещений"
    });
});

var app = builder.Build();

// Применяем миграции при старте приложения
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking API v1");
        c.RoutePrefix = "swagger"; // Теперь Swagger будет доступен по /swagger
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Временный эндпоинт для просмотра таблиц
app.MapGet("/debug/tables", async (ApplicationDbContext context) =>
{
    var tables = new
    {
        Users = await context.Users.ToListAsync(),
        Rooms = await context.Rooms.ToListAsync(),
        Bookings = await context.Bookings.ToListAsync(),
        BookingRooms = await context.BookingRooms.ToListAsync()
    };
    return Results.Json(tables);
});

app.Run();