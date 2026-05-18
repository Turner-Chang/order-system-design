using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSystem.Domain;

namespace OrderSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core 對應設定（Day 4.3）：告訴 EF Core 怎麼還原 OrderItem。
///
/// OrderItem 是 Order Aggregate 內的 Entity（不是 Aggregate Root），
/// 所以不應該直接 query（DbContext 沒有 DbSet&lt;OrderItem&gt;），
/// 但 EF Core 仍需要知道怎麼把 OrderItem 的資料表 schema 建出來、
/// 以及怎麼從 DB 還原 OrderItem 物件。
///
/// 這個檔案處理「OrderItem 自己」的設定，
/// Order ↔ OrderItem 的關聯設定也放在這（按業界慣例，從屬端宣告關聯）。
/// </summary>
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        // ====================================================================
        // 決策點 #6：OrderItem 沒有 Id 屬性，怎麼當 Primary Key？
        // ────────────────────────────────────────────────────────────────────
        // 你的 OrderItem.cs 只有 ProductId / UnitPrice / Quantity——沒有自己的 Id。
        // 這在 DDD 上是「Value Object」的設計（沒有獨立識別），
        // 但 EF Core 要把它存進 DB 表，**每個 row 必須有 primary key**。
        //
        // 三條路（各有取捨）：
        //   (a) 不加 Id 屬性、用 EF Core「shadow property」幫你加一個隱形的 Id
        //       → Domain 完全乾淨，但 EF Core 內部知道有 Id
        //       → builder.HasKey("Id"); （字串型——shadow property）
        //
        //   (b) 用「複合鍵」（OrderId + ProductId）當 PK
        //       → DDD 更純：OrderItem 就是「某 Order 裡的某 Product」
        //       → 但同一筆 Order 不能有同一個 ProductId 兩次（業務上合理嗎？）
        //
        //   (c) 在 Domain OrderItem 加 public Guid Id，跟 Order 一樣
        //       → 簡單，但 Domain 設計變成「OrderItem 也是有獨立識別的 Entity」
        //       → 跟原本 Value Object 直覺有點偏離
        //
        // 推薦：(a) shadow property——維持 Domain 純淨、EF Core 內部處理 Id 細節。
        //       (a) 也是業界 Clean Architecture 對「Aggregate 內 Entity」的主流做法。
        //
        //  ✅ 最後決定C
        //     在 OrderItem 加 public Guid Id，當 PK，將OrderItem也是唯一個Entity
        //     將每個商品都視為一個Entity，讓它有自己的識別，這樣也比較符合實際業務情況
        // ====================================================================
        builder.HasKey(oi => oi.Id);


        // ====================================================================
        // 決策點 #7：UnitPrice 的 decimal 精度
        // ────────────────────────────────────────────────────────────────────
        // C# decimal 跟 DB decimal 是兩個世界。
        // SQLite 對 decimal 的處理：實際存 TEXT、用 string 表示精確值。
        // 不設定也能跑，但會跳警告（運行時 + migration 階段）：
        //   "The 'decimal' property 'UnitPrice' is part of a key on entity type 'OrderItem'.
        //    If the configured precision and scale don't match the column type,
        //    values may be truncated."
        //
        // 業界紀律：金額類 decimal 設定 (18, 2) ——18 位總數、2 位小數。
        //   18 位含小數 → 整數部分 16 位 → 可表達到 9,999,999,999,999,999.99 ≈ 1 京
        //   2 位小數 → 「分」（台幣不用小數可設 (18, 0)，國際幣別 2 位小數）
        //
        // 解法：builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        //
        //  ✅ 寫一行 HasPrecision
        // ====================================================================
        builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);


        // ====================================================================
        // 決策點 #8：Subtotal 計算屬性要 Ignore
        // ────────────────────────────────────────────────────────────────────
        // OrderItem.Subtotal => UnitPrice * Quantity 跟 Order.TotalAmount 同性質——
        // 計算屬性、沒 setter、從現有欄位推導。
        // 不 Ignore → EF Core 試圖 map → migration 跑掉。
        //
        // 解法：builder.Ignore(oi => oi.Subtotal);
        //
        // ✅ 寫一行 Ignore
        // ====================================================================
        builder.Ignore(oi => oi.Subtotal);


        // ====================================================================
        // 決策點 #9：Order ↔ OrderItem 一對多關聯
        // ────────────────────────────────────────────────────────────────────
        // 業界慣例：關聯設定寫在「從屬端」（OrderItem 是從屬於 Order 的）。
        // 你的 Domain OrderItem 沒有 OrderId 屬性也沒有 Order navigation property，
        // 但 EF Core 在資料表還是會自動加一個 OrderId column（shadow property 形式）
        // 把 OrderItem 連回 Order。
        //
        // 關鍵 API（鏈式）：
        //   builder.HasOne<Order>()              ← 從屬端指向主端（無 navigation prop 寫無泛型）
        //          .WithMany(o => o.Items)        ← 主端的集合屬性
        //          .HasForeignKey("OrderId")      ← shadow FK 欄位名
        //          .OnDelete(DeleteBehavior.Cascade);  ← Order 被刪時 Items 跟著刪
        //
        // 補充：你 Order.Items 是 IReadOnlyList，EF Core 找 navigation 還是會走 _items 欄位
        //       （前面 OrderConfiguration 已設定 backing field），所以 .WithMany(o => o.Items) 能寫
        //
        //  ✅ 寫上面四行鏈式呼叫
        //      Cascade 是合理預設（Order 沒了，它的 Items 也不該存在）；
        //      你也可以選 Restrict/SetNull，但對業務沒對應意義。
        // ====================================================================
        builder.HasOne<Order>()
               .WithMany(o => o.Items)
               .HasForeignKey("OrderId")
               .OnDelete(DeleteBehavior.Cascade);
    }
}
