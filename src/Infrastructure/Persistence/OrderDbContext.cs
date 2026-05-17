using Microsoft.EntityFrameworkCore;
using OrderSystem.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSystem.Infrastructure.Persistence
{
    // EF Core 強制樣板：OrderDbContext 繼承自 Entity Framework Core 的 DbContext
    public class OrderDbContext : DbContext
    {
        // Set<T>此寫法也可避免在外部被指向其他物件，並且值由 EF Core 內部 cache 動態提供，不需要 backing field 存值
        public DbSet<Order> Orders => Set<Order>();

        // EF Core 強制樣板：Constructor 必須接 DbContextOptions<TContext>，pass 到 base
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        // EF Core 強制樣板：OnModelCreating override 簽章
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
    }
}
