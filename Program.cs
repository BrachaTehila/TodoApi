using Microsoft.EntityFrameworkCore;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.WithOrigins("https://todolistreact-buof.onrender.com") // ודא שזו כתובת הלקוח הנכונה
               .AllowAnyMethod()
               .AllowAnyHeader());
});

// Register the DbContext as a service
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"),
    Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.40-mysql")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");

// טיפול בשגיאות גלובלי
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\":\"An unexpected error occurred.\"}");
    });
});


    app.UseSwagger();
    app.UseSwaggerUI();


app.MapGet("/", () => "TodoApi is running");
app.MapGet("/tasks", async (ToDoDbContext dbContext) =>
{
    try
    {
        var tasks = await dbContext.Items.ToListAsync();
        return Results.Ok(tasks);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving tasks: {ex.Message}");
    }
});

app.MapPost("/tasks", async (ToDoDbContext dbContext, TodoApi.Item newTask) =>
{
    try
    {
        newTask.Id = 0;
        dbContext.Items.Add(newTask);
        await dbContext.SaveChangesAsync();
        return Results.Created($"/tasks/{newTask.Id}", newTask);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error creating task: {ex.Message}");
    }
});

app.MapPut("/tasks/{id}", async (ToDoDbContext dbContext, int id, Item updatedTask) =>
{
    try
    {
        var task = await dbContext.Items.FindAsync(id);
        if (task == null)
            return Results.NotFound($"Task with ID {id} not found.");

        task.Name = updatedTask.Name;
        task.IsComplete = updatedTask.IsComplete;

        await dbContext.SaveChangesAsync();
        return Results.Ok(task);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error updating task: {ex.Message}");
    }
});

app.MapDelete("/tasks/{id}", async (ToDoDbContext dbContext, int id) =>
{
    try
    {
        var task = await dbContext.Items.FindAsync(id);
        if (task == null)
            return Results.NotFound($"Task with ID {id} not found.");

        dbContext.Items.Remove(task);
        await dbContext.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error deleting task: {ex.Message}");
    }
});

app.Run();
