using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<DeliveryNote> DeliveryNotes { get; set; }
        public DbSet<DeliveryDetail> DeliveryDetails { get; set; }
        public DbSet<ReceiveRecord> ReceiveRecords { get; set; }
        public DbSet<ReceiveDetail> ReceiveDetails { get; set; }
        public DbSet<Inventory> Inventories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================
            // User 配置
            // =====================================
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
                entity.HasKey(e => e.UserID);
                entity.HasIndex(e => e.UserCode).IsUnique();
            });

            // =====================================
            // Role 配置
            // =====================================
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");
                entity.HasKey(e => e.RoleID);
            });

            // =====================================
            // UserRole 配置
            // =====================================
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRole");
                entity.HasKey(e => e.UserRoleID);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(e => e.RoleID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================
            // Supplier 配置
            // =====================================
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Supplier");
                entity.HasKey(e => e.SupplierID);
            });

            // =====================================
            // Material 配置
            // =====================================
            modelBuilder.Entity<Material>(entity =>
            {
                entity.ToTable("Material");
                entity.HasKey(e => e.MaterialID);
            });

            // =====================================
            // Warehouse 配置
            // =====================================
            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.ToTable("Warehouse");
                entity.HasKey(e => e.WareID);
            });

            // =====================================
            // PurchaseOrder 配置
            // 注意: 对 User 有两个外键（CreateByID, UpdateByID）
            // =====================================
            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.ToTable("PurchaseOrder");
                entity.HasKey(e => e.OrderID);

                entity.HasOne(e => e.Supplier)
                      .WithMany(s => s.PurchaseOrders)
                      .HasForeignKey(e => e.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreateByUser)
                      .WithMany(u => u.CreatedPurchaseOrders)
                      .HasForeignKey(e => e.CreateByID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UpdateByUser)
                      .WithMany(u => u.UpdatedPurchaseOrders)
                      .HasForeignKey(e => e.UpdateByID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =====================================
            // OrderDetail 配置
            // =====================================
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.ToTable("OrderDetail");
                entity.HasKey(e => e.OrderDetailID);

                entity.HasOne(e => e.PurchaseOrder)
                      .WithMany(p => p.OrderDetails)
                      .HasForeignKey(e => e.OrderID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================
            // DeliveryNote 配置
            // =====================================
            modelBuilder.Entity<DeliveryNote>(entity =>
            {
                entity.ToTable("DeliveryNote");
                entity.HasKey(e => e.NoteID);

                entity.HasOne(e => e.PurchaseOrder)
                      .WithMany(p => p.DeliveryNotes)
                      .HasForeignKey(e => e.OrderID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Supplier)
                      .WithMany(s => s.DeliveryNotes)
                      .HasForeignKey(e => e.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreateByUser)
                      .WithMany(u => u.CreatedDeliveryNotes)
                      .HasForeignKey(e => e.CreateByID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =====================================
            // DeliveryDetail 配置
            // =====================================
            modelBuilder.Entity<DeliveryDetail>(entity =>
            {
                entity.ToTable("DeliveryDetail");
                entity.HasKey(e => e.DeliveryDetailID);

                entity.HasOne(e => e.DeliveryNote)
                      .WithMany(d => d.DeliveryDetails)
                      .HasForeignKey(e => e.NoteID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================
            // ReceiveRecord 配置
            // =====================================
            modelBuilder.Entity<ReceiveRecord>(entity =>
            {
                entity.ToTable("ReceiveRecord");
                entity.HasKey(e => e.ReceiveID);

                entity.HasOne(e => e.DeliveryNote)
                      .WithMany(d => d.ReceiveRecords)
                      .HasForeignKey(e => e.NoteID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Supplier)
                      .WithMany(s => s.ReceiveRecords)
                      .HasForeignKey(e => e.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ReceiveUser)
                      .WithMany(u => u.ReceiveRecords)
                      .HasForeignKey(e => e.ReceiveUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =====================================
            // ReceiveDetail 配置
            // =====================================
            modelBuilder.Entity<ReceiveDetail>(entity =>
            {
                entity.ToTable("ReceiveDetail");
                entity.HasKey(e => e.ReceiveDetailID);

                entity.HasOne(e => e.ReceiveRecord)
                      .WithMany(r => r.ReceiveDetails)
                      .HasForeignKey(e => e.ReceiveID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.DeliveryDetail)
                      .WithMany(d => d.ReceiveDetails)
                      .HasForeignKey(e => e.DeliveryDetailID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Material)
                      .WithMany(m => m.ReceiveDetails)
                      .HasForeignKey(e => e.MaterialID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreateByUser)
                      .WithMany()
                      .HasForeignKey(e => e.CreateBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // =====================================
            // Inventory 配置
            // =====================================
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.ToTable("Inventory");
                entity.HasKey(e => e.InventoryID);
                entity.HasIndex(e => new { e.WareID, e.MaterialID }).IsUnique();

                entity.HasOne(e => e.Material)
                      .WithMany(m => m.Inventories)
                      .HasForeignKey(e => e.MaterialID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Warehouse)
                      .WithMany(w => w.Inventories)
                      .HasForeignKey(e => e.WareID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UpdateByUser)
                      .WithMany(u => u.UpdatedInventories)
                      .HasForeignKey(e => e.UpdateByID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
