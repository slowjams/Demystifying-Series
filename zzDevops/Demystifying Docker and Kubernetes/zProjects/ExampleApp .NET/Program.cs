using ExampleApp.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:80");

// Add services to the container.
builder.Services.AddControllersWithViews();

var host = builder.Configuration["DBHOST"] ?? "localhost";
var port = builder.Configuration["DBPORT"] ?? "3306";
var password = builder.Configuration["DBPASSWORD"] ?? "mysecret";
var connString = $"server={host};userid=root;pwd={password};" + $"port={port};database=products;SSL Mode=None";
builder.Services.AddDbContext<ProductDbContext>(options =>
    // AutoDetect needs to connect to mysql when creating a migration, because of it, you have to add `-p 3306:3306` into commands below
    // `docker run -d --name mysql -v productdata:/var/lib/mysql -e MYSQL_ROOT_PASSWORD=mysecret -e bind-address=0.0.0.0 mysql:8.0.0`
    // note that for this asp.net application to connect to mysql, it doesn't need mysql instance to expose 3306 to outside 
    // since the app and mysql instance are in same bridge network when you run `docker run -d --name productapp -p 3000:80 -e DBHOST=172.17.0.2 apress/exampleapp`
    options.UseMySql(connString, ServerVersion.AutoDetect(connString))
);
builder.Services.AddTransient<IRepository, ProductRepository>();
//builder.Services.AddTransient<IRepository, DummyRepository>();

/*
builder.Configuration.AddCommandLine(args);
if ((builder.Configuration["INITDB"] ?? "false") == "true")
{
    Console.WriteLine("Preparing Database...");
    SeedData.EnsurePopulated(new ProductDbContext());
    Console.WriteLine("Database Preparation Complete");

    return;
}
*/

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    SeedData.EnsurePopulated(app);
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
