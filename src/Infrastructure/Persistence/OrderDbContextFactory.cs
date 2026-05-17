using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OrderSystem.Infrastructure.Persistence
{
    // 在業界標準部屬流程是： development 機器上跑 migration → commit 進 git → deploy 階段套用
    // 因此在 development 階段，EF Core 需要一個工廠來建立 DbContext 實例，這樣 EF Core CLI 工具才能正確地執行 migration 命令
    // IDesignTimeDbContextFactory——dotnet ef CLI 工具的逃生口。
    // migration 跑時 Program.cs 沒啟動、DI 容器不存在，所以靠這個 factory 自己讀 appsettings.json 建出 DbContext。
    // Runtime（App 啟動）建 DbContext 走 Api/Program.cs 的 DI 註冊，跟這個 factory 無關。

    public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
    {
        public OrderDbContext CreateDbContext(string[] args)
        {
            // Step 1 ：建立 ConfigurationBuilder 讀 appsettings.json
            var configuration =   new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json",optional:false)
                .Build();

            // Step 2 : 從 appsettings.json 讀取 ConnectionString
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // Step 3 : 建立 DbContextOptionsBuilder 並設定 ConnectionString
            var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>().UseSqlite(connectionString);

            // Step 4 : 回傳 OrderDbContext 實例
            return new OrderDbContext(optionsBuilder.Options);


        }
    }
}
