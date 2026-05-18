using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSystem.Domain;

namespace OrderSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core 對應設定（Day 4.3）：告訴 EF Core 怎麼還原 Order 這個 Aggregate Root。
/// 跟 Order.cs Domain 物件本身 0 修改——所有「Persistence 適配」都關在這個檔案內。
/// 對應 shared-concepts/ef-core-fundamentals.md §6 OnModelCreating。
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // ====================================================================
        // 決策點 #1：Primary Key
        // ────────────────────────────────────────────────────────────────────
        // Order.Id 是 Guid，這是聚合根的識別欄位。
        // 問題：EF Core 預設靠「屬性名稱 Id 或 {EntityName}Id」自動推斷 key，
        //       Order.Id 符合慣例 → 預設能找到。
        //       但「依賴慣例」vs「顯式宣告」是工程紀律問題。
        // ✅ 寫一行 builder.HasKey(...) 顯式宣告 Order.Id 為 PK
        // ====================================================================
        builder.HasKey(o => o.Id);

        // ====================================================================
        // 決策點 #2：Items navigation property 走 backing field
        // ────────────────────────────────────────────────────────────────────
        // 這是 Day 4.3 最重要的設定——對應你 Day 3 的 4 層封裝。
        // Order 暴露 IReadOnlyList<OrderItem> Items（介面層保護），
        // 底層是 private readonly List<OrderItem> _items。
        // EF Core 預設找 public setter → 找不到（你刻意擋住）。
        // 解法：顯式告訴 EF Core「Items 屬性的後門欄位叫 _items，請走後門」。
        //
        // 涉及兩個方法（鏈式呼叫）：
        //   builder.Navigation(o => o.Items)
        //          .HasField("_items")               ← 指定 backing field 名稱
        //          .UsePropertyAccessMode(PropertyAccessMode.Field);  ← 讀寫都走欄位
        //
        // 設定後 EF Core 從 DB 還原時：
        //   - 直接寫進 _items.Add(...)，跳過 AddItem 業務驗證 ✅
        //   - 業務 code 仍然只能走 Order.AddItem() ✅（4 層封裝保留）
        // 
        // 這個設定的哲學意義：EF Core 是搬家工人，Order 是房子，Items 是房子裡的東西。
        // EF Core 是來「搬東西進屋」的搬家工人（從 DB 還原資料）。
        // Navigation(o => o.Items) = 告訴工人「我們在處理 Items 這棟房子」
        // HasField("_items") = 告訴工人「這棟房子有後門，後門編號 _items」
        // UsePropertyAccessMode(...) = 告訴工人「搬的時候請從後門進、不要走大門」
        //
        // ✅ 寫上面三行鏈式呼叫
        // ====================================================================
        builder.Navigation(o => o.Items)                   // 我們在處理：Items 這個 Navigation
               .HasField("_items")                               // 它的後門欄位叫 _items
               .UsePropertyAccessMode(PropertyAccessMode.Field); // 存取策略改成：讀寫一律走欄位（不走屬性）


        // ====================================================================
        // 決策點 #3：TotalAmount 計算屬性要 Ignore
        // ────────────────────────────────────────────────────────────────────
        // Order.TotalAmount => _items.Sum(i => i.Subtotal) 是計算屬性，
        // 沒有 setter、值從 _items 推導出來。
        // 如果不告訴 EF Core 忽略，它會誤以為要 map 一個 TotalAmount 欄位進 DB
        // → migration 會建一個 TotalAmount column，runtime 嘗試寫入時報錯。
        //
        // 解法：builder.Ignore(o => o.TotalAmount);
        //
        // 哲學連結：「值現算」vs「值快取」（呼應你今天問的 Cache 質疑庫 Q4）。
        // TotalAmount 從 items 算 → DB 只存 items 是來源真實值 → 永遠一致 ✅
        //
        // ✅ 寫一行 Ignore
        // ====================================================================
        builder.Ignore(o => o.TotalAmount);


        // ====================================================================
        // 決策點 #4：OrderStatus 存成 string（不是預設的 int）
        // ────────────────────────────────────────────────────────────────────
        // OrderStatus 是 enum（Pending / Paid / Shipped / Completed / Cancelled）。
        // EF Core 預設存 int（0/1/2/3/4）——能跑、空間小、但 DB 直接看時看不懂。
        //
        // 兩種選擇：
        //   (a) 預設 int：簡潔、效能微優，但要對照 enum 定義才知道 3 是什麼
        //   (b) 存 string：DB 直接看就懂、enum 改順序不破壞舊資料、業界 Clean Architecture 主流
        //
        // 你今天 HANDOVER 已決定走 (b)——這次只是把它寫出來。
        // 
        // 解法：builder.Property(o => o.Status).HasConversion<string>();
        //
        //  ✅ 寫一行 HasConversion<string>()
        // ====================================================================
        // Status 這個屬性，存進 DB 時用字串、讀出時自動轉回 enum
        // HasConversion<string>() 是內建好的「enum ↔ string」converter
        // 寫 HasConversion<string>() — DB 存 "Paid" 而非 1，避免 enum 改順序破壞舊資料
        // 透過直接寫入「名稱」而不是寫入「代號」的方式，後續在DB進行查詢時，可以省略回程式碼查看Enum的定義
        builder.Property(o => o.Status).HasConversion<string>(); 


        // ====================================================================
        // 決策點 #5：（可選）資料表名稱顯式宣告
        // ────────────────────────────────────────────────────────────────────
        // EF Core 預設用 DbSet 屬性名稱當資料表名（Orders）。
        // 顯式 ToTable("Orders") 是「顯式 > 隱式」工程紀律的延伸。
        // 也可以選擇不寫（依賴慣例）。這個決策點影響很小，自己選。
        //
        // 解法（如果你想顯式）：builder.ToTable("Orders");
        //
        // ✅ 不寫——SQLite 無 schema 需求、DbSet 屬性名穩定，依賴慣例足夠
        // （若未來自訂 schema 或表名（如 dbo.Orders / tbl_Order）再加 builder.ToTable("xxxx")）
        // ====================================================================

    }
}
