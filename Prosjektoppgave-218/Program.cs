using Prosjektoppgave_218.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<MapService>();
builder.Services.AddLogging(builder =>
{
    builder.AddConsole(); // Log to the console
    builder.AddDebug();   // Log to the debug output
                          // You can add other logging providers here (e.g., Application Insights, file logging)
});

// Register the PowerPlantService
builder.Services.AddScoped<Prosjektoppgave_218.Services.MapService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Map}/{action=Map}/{id?}");

app.Run();
