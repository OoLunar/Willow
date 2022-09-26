using Microsoft.EntityFrameworkCore;
using OoLunar.Willow.Database.Models;

namespace OoLunar.Willow.Database
{
    public sealed class DatabaseContext : DbContext
    {
        public DbSet<UserModel> Users { get; init; } = null!;
        public DbSet<LoginModel> Logins { get; init; } = null!;

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite("Data Source=database.db");
    }
}
