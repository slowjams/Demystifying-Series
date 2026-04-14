using Microsoft.EntityFrameworkCore;

namespace ExampleApp.Models
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext() { }

        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var envs = Environment.GetEnvironmentVariables();
            var host = envs["DBHOST"] ?? "localhost";
            var port = envs["DBPORT"] ?? "3306";
            var password = envs["DBPASSWORD"] ?? "mysecret";
            var connString = $"server={host};userid=root;pwd={password};" + $"port={port};database=products;SSL Mode=None";
            options.UseMySql(connString, ServerVersion.AutoDetect(connString));
        }
    }
}