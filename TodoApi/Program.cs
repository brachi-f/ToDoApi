using System;
using System.Security.Claims;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
builder.Services.AddDbContext<ToDoDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ToDoDB");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

var app = builder.Build();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger";
});

app.MapGet(
    "/items",
    async (ToDoDbContext dbContext) =>
    {
        var tasks = await dbContext.Items.ToListAsync();
        return Results.Ok(tasks);
    }
);

app.MapPost(
    "/items",
    async (ToDoDbContext dbContext, Item item) =>
    {
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();
        return Results.Created($"/items/{item.Id}", item);
    }
);

app.MapPut(
    "/items/{id}",
    async (ToDoDbContext dbContext, int id, bool isComplete) =>
    {
        var oldItem = await dbContext.Items.FindAsync(id);
        if (oldItem == null)
            return Results.NotFound();
        oldItem.IsComplete = isComplete;
        dbContext.Items.Update(oldItem);
        await dbContext.SaveChangesAsync();
        return Results.Ok(oldItem);
    }
);

app.MapDelete(
    "/items/{id}",
    async (ToDoDbContext dbContext, int id) =>
    {
        var item = await dbContext.Items.FindAsync(id);
        if (item == null)
            return Results.NotFound();
        dbContext.Remove(item);
        await dbContext.SaveChangesAsync();
        return Results.Ok();
    }
);


app.Run();

