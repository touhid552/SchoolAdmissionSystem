using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Api.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection.");
var holdSeconds = int.TryParse(builder.Configuration["HOLD_DURATION_SECONDS"], out var seconds) && seconds > 0 ? seconds : 600;
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); await db.Database.MigrateAsync(); }
app.UseCors();
app.Use(async (ctx, next) => {
    try { await next(); }
    catch (Exception ex) {
        app.Logger.LogError(ex, "Request failed");
        if (!ctx.Response.HasStarted) { ctx.Response.StatusCode = 500; await ctx.Response.WriteAsJsonAsync(new { error = "Something went wrong. Please try again." }); }
    }
});
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

static string NewId(string prefix) => prefix + "_" + Guid.NewGuid().ToString("N");
static IResult Error(int code, string message) => Results.Json(new { error = message }, statusCode: code);
static bool StudentOk(HttpContext c, out string id) {
    id = c.Request.Headers["X-Student-Id"].ToString();
    return System.Text.RegularExpressions.Regex.IsMatch(id, @"^[0-9]{17}$");
}
static async Task Expire(AppDbContext db, string? classId = null) {
    var now = DateTime.UtcNow;
    var q = db.Holds.Where(h => h.Status == HoldStatus.Held && h.ExpiresAt <= now);
    if (classId != null) q = q.Where(h => h.ClassId == classId);
    await q.ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired));
}
static async Task<object> ClassDto(AppDbContext db, SchoolClass c) {
    await Expire(db, c.Id);
    var held = await db.Holds.CountAsync(h => h.ClassId == c.Id && h.Status == HoldStatus.Held);
    var confirmed = await db.Holds.CountAsync(h => h.ClassId == c.Id && h.Status == HoldStatus.Confirmed);
    return new { id=c.Id, name=c.Name, totalSeats=c.TotalSeats, heldSeats=held, confirmedSeats=confirmed, availableSeats=c.TotalSeats-held-confirmed };
}
app.MapGet("/classes", async Task<IResult> (AppDbContext db) => {
    var classes = await db.Classes.OrderBy(c=>c.CreatedAt).ToListAsync();
    var result = new List<object>(); foreach(var c in classes) result.Add(await ClassDto(db,c));
    return Results.Ok(result);
});
app.MapGet("/classes/{classId}", async Task<IResult> (string classId, AppDbContext db) => {
    var c = await db.Classes.FindAsync(classId); return c is null ? Error(404,"Class not found.") : Results.Ok(await ClassDto(db,c));
});
app.MapPost("/classes", async Task<IResult> (ClassInput input, AppDbContext db) => {
    var name = input.Name?.Trim();
    if (string.IsNullOrWhiteSpace(name) || name.Length>200 || input.TotalSeats<1 || input.TotalSeats>100000)
        return Error(400,"Enter a class name and seat capacity between 1 and 100000.");
    var c = new SchoolClass { Id=NewId("c"), Name=name, TotalSeats=input.TotalSeats, CreatedAt=DateTime.UtcNow };
    db.Classes.Add(c); await db.SaveChangesAsync();
    return Results.Created($"/classes/{c.Id}", new { id=c.Id, name=c.Name, totalSeats=c.TotalSeats });
});
app.MapPost("/classes/{classId}/holds", async Task<IResult> (string classId, HttpContext ctx, AppDbContext db) => {
    if (!StudentOk(ctx,out var student)) return Error(400,"Enter a valid 17-digit birth registration number.");
    await using var tx = await db.Database.BeginTransactionAsync();
    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Classes\" WHERE \"Id\" = {classId} FOR UPDATE");
    var c = await db.Classes.FindAsync(classId);
    if (c is null) return Error(404,"Class not found.");
    await Expire(db,classId);
    var duplicate = await db.Holds.AnyAsync(h=>h.ClassId==classId && h.StudentId==student && (h.Status==HoldStatus.Held || h.Status==HoldStatus.Confirmed));
    if (duplicate) return Error(409,"You already have an active application for this class.");
    var held = await db.Holds.CountAsync(h=>h.ClassId==classId && h.Status==HoldStatus.Held);
    var confirmed = await db.Holds.CountAsync(h=>h.ClassId==classId && h.Status==HoldStatus.Confirmed);
    if (held+confirmed>=c.TotalSeats) return Error(409,"Sorry, no seats are currently available.");
    var now=DateTime.UtcNow; var h=new Hold { Id=NewId("h"), ClassId=classId, StudentId=student, Status=HoldStatus.Held, CreatedAt=now, ExpiresAt=now.AddSeconds(holdSeconds) };
    db.Holds.Add(h); await db.SaveChangesAsync(); await tx.CommitAsync();
    return Results.Created($"/holds/{h.Id}",new { holdId=h.Id,classId=h.ClassId,studentId=h.StudentId,status=h.Status,expiresAt=h.ExpiresAt });
});
app.MapGet("/holds/{holdId}", async Task<IResult> (string holdId, HttpContext ctx, AppDbContext db) => {
    var h=await db.Holds.FindAsync(holdId); if(h is null) return Error(404,"Application not found.");
    if(ctx.Request.Headers.ContainsKey("X-Student-Id")) {
        if(!StudentOk(ctx,out var sid) || sid!=h.StudentId) return Error(404,"Application not found.");
    }
    if(h.Status==HoldStatus.Held && h.ExpiresAt<=DateTime.UtcNow) { h.Status=HoldStatus.Expired; await db.SaveChangesAsync(); }
    return Results.Ok(new { holdId=h.Id,classId=h.ClassId,status=h.Status,expiresAt=h.ExpiresAt });
});
app.MapPost("/holds/{holdId}/confirm", async Task<IResult> (string holdId,HttpContext ctx,AppDbContext db) => {
    if(!StudentOk(ctx,out var student)) return Error(400,"Enter a valid 17-digit birth registration number.");
    var initial=await db.Holds.AsNoTracking().FirstOrDefaultAsync(h=>h.Id==holdId);
    if(initial is null || initial.StudentId!=student) return Error(404,"Application not found.");
    await using var tx=await db.Database.BeginTransactionAsync();
    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Classes\" WHERE \"Id\" = {initial.ClassId} FOR UPDATE");
    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Holds\" WHERE \"Id\" = {holdId} FOR UPDATE");
    var h=await db.Holds.FindAsync(holdId);
    if(h is null || h.StudentId!=student) return Error(404,"Application not found.");
    if(h.Status==HoldStatus.Confirmed) { await tx.CommitAsync(); return Results.Ok(new {holdId=h.Id,status=h.Status}); }
    if(h.Status!=HoldStatus.Held) return Error(409,"This application is no longer active.");
    if(h.ExpiresAt<=DateTime.UtcNow) { h.Status=HoldStatus.Expired; await db.SaveChangesAsync(); await tx.CommitAsync(); return Error(409,"Your seat hold has expired. Please apply again."); }
    h.Status=HoldStatus.Confirmed; await db.SaveChangesAsync(); await tx.CommitAsync();
    return Results.Ok(new {holdId=h.Id,status=h.Status});
});
app.MapDelete("/holds/{holdId}", async Task<IResult> (string holdId,HttpContext ctx,AppDbContext db) => {
    if(!StudentOk(ctx,out var student)) return Error(400,"Enter a valid 17-digit birth registration number.");
    var initial=await db.Holds.AsNoTracking().FirstOrDefaultAsync(h=>h.Id==holdId);
    if(initial is null || initial.StudentId!=student) return Error(404,"Application not found.");
    await using var tx=await db.Database.BeginTransactionAsync();
    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Classes\" WHERE \"Id\" = {initial.ClassId} FOR UPDATE");
    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Holds\" WHERE \"Id\" = {holdId} FOR UPDATE");
    var h=await db.Holds.FindAsync(holdId);
    if(h is null || h.StudentId!=student) return Error(404,"Application not found.");
    if(h.Status==HoldStatus.Cancelled) { await tx.CommitAsync(); return Results.NoContent(); }
    if(h.Status!=HoldStatus.Held) return Error(409,"Only a held application can be cancelled.");
    h.Status=HoldStatus.Cancelled; await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.NoContent();
});
app.MapGet("/me/holds", async Task<IResult> (HttpContext ctx,AppDbContext db) => {
    if(!StudentOk(ctx,out var student)) return Error(400,"Enter a valid 17-digit birth registration number.");
    await Expire(db);
    var rows=await (from h in db.Holds join c in db.Classes on h.ClassId equals c.Id where h.StudentId==student orderby h.CreatedAt descending select new {holdId=h.Id,classId=h.ClassId,className=c.Name,status=h.Status,expiresAt=h.ExpiresAt}).ToListAsync();
    return Results.Ok(rows);
});
app.Run();

record ClassInput(string? Name,int TotalSeats);
