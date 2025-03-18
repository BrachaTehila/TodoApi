using Microsoft.EntityFrameworkCore;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.WithOrigins("http://localhost:3000")  // כתובת הלקוח שלך
               .AllowAnyMethod()
               .AllowAnyHeader());
});

// Register the DbContext as a service
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ToDoDB")))); // Adjust according to your SQL provider
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors("AllowAll");
// if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
// {
    app.UseSwagger(); // מייצר את ה-Swagger JSON
    app.UseSwaggerUI(); // מציג את ממשק ה-UI של Swagger
// }
app.MapGet("/", ()=>"Hello World!");
app.MapGet("/tasks", async (ToDoDbContext dbContext) =>
{
    var tasks = await dbContext.Items.ToListAsync(); // Retrieve all items from the database
    return Results.Ok(tasks);
});

app.MapPost("/tasks", async (ToDoDbContext dbContext, TodoApi.Item newTask) =>
{
    newTask.Id = 0; // ID חדש ייווצר אוטומטית
    dbContext.Items.Add(newTask); // הוספת המשימה לטבלה items
    await dbContext.SaveChangesAsync(); // שמירת השינויים
    return Results.Created($"/tasks/{newTask.Id}", newTask);
});

// עדכון משימה
app.MapPut("/tasks/{id}", async (ToDoDbContext dbContext, int id, Item updatedTask) =>
{
    var task = await dbContext.Items.FindAsync(id); // מציאת המשימה בטבלה items
    if (task == null)
        return Results.NotFound($"Task with ID {id} not found.");

    task.Name = updatedTask.Name;
    task.IsComplete = updatedTask.IsComplete;

    await dbContext.SaveChangesAsync(); // שמירת השינויים
    return Results.Ok(task);
});

// מחיקת משימה
app.MapDelete("/tasks/{id}", async (ToDoDbContext dbContext, int id) =>
{
    var task = await dbContext.Items.FindAsync(id); // חיפוש המשימה ב-items
    if (task == null)
        return Results.NotFound($"Task with ID {id} not found.");

    dbContext.Items.Remove(task); // מחיקת המשימה
    await dbContext.SaveChangesAsync(); // שמירת השינויים
    return Results.NoContent();
});

// Route ברירת מחדל
app.MapGet("/", () => "TodoApi is running");

app.Run();
