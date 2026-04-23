using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var notes = new List<Note>();
var nextId = 1;

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        time = DateTime.UtcNow
    });
});

app.MapGet("/version", (IConfiguration config) =>
{
    return Results.Ok(new
    {
        name = config["App:Name"] ?? "IsLabApp",
        version = config["App:Version"] ?? "0.1.0"
    });
});

app.MapGet("/api/notes", () =>
{
    return Results.Ok(notes);
});

app.MapGet("/api/notes/{id:int}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);

    return note is null
        ? Results.NotFound(new { message = "Note not found" })
        : Results.Ok(note);
});

app.MapPost("/api/notes", (CreateNoteRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { message = "Title is required" });
    }

    var note = new Note(
        nextId++,
        request.Title.Trim(),
        request.Text?.Trim(),
        DateTime.UtcNow
    );

    notes.Add(note);

    return Results.Created($"/api/notes/{note.Id}", note);
});

app.MapDelete("/api/notes/{id:int}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);

    if (note is null)
    {
        return Results.NotFound(new { message = "Note not found" });
    }

    notes.Remove(note);

    return Results.Ok(new { message = "Deleted" });
});

app.MapGet("/db/ping", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Mssql");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Ok(new
        {
            status = "error",
            message = "Connection string 'Mssql' is empty"
        });
    }

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();

        return Results.Ok(new
        {
            status = "ok",
            db = "reachable",
            result
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            status = "error",
            message = ex.Message
        });
    }
});

app.Run();

public record Note(int Id, string Title, string? Text, DateTime CreatedAt);

public record CreateNoteRequest(string Title, string? Text);