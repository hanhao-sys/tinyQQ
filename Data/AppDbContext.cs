using Microsoft.EntityFrameworkCore;
using tinyQQ.Web.Models;

namespace tinyQQ.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Friend> Friends => Set<Friend>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageView> MessageView => Set<MessageView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // v_Message 是数据库视图，EF Core 视为只读
        modelBuilder.Entity<MessageView>(entity =>
        {
            entity.ToView("v_Message");
            entity.HasNoKey();
        });
    }
}
