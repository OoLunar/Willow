using System;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using OoLunar.Willow.Models;

namespace OoLunar.Willow.Server
{
    public sealed class DatabaseContext : DbContext, IDesignTimeDbContextFactory<DatabaseContext>
    {
        public DbSet<UserModel> Users { get; init; } = null!;
        public DbSet<LoginModel> Logins { get; init; } = null!;
        public DbSet<CommandModel> Commands { get; init; } = null!;

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite("Data Source=database.db");

        public DatabaseContext CreateDbContext(string[] args)
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.Sources.Clear();
            configurationBuilder.AddJsonFile(Path.Join(Environment.CurrentDirectory, "config.json"), true, true);
            configurationBuilder.AddEnvironmentVariables("WILLOW_");
            configurationBuilder.AddCommandLine(args);
            IConfigurationRoot configuration = configurationBuilder.Build();

            DbContextOptionsBuilder<DatabaseContext> optionsBuilder = new();
            SqliteConnectionStringBuilder connectionBuilder = new()
            {
                DataSource = configuration["Database:Path"] ?? "database.db",
                Password = configuration.GetValue<string?>("database:password")
            };
            optionsBuilder.UseSnakeCaseNamingConvention(CultureInfo.InvariantCulture);
            optionsBuilder.UseSqlite(connectionBuilder.ToString());

            DatabaseContext databaseContext = new(optionsBuilder.Options);
            return databaseContext;
        }
    }
}
