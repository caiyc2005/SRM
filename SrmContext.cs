using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace backend;

public partial class SrmContext : DbContext
{
    public SrmContext()
    {
    }

    public SrmContext(DbContextOptions<SrmContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=SRM;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE3ACAD4EB42");

            entity.ToTable("Role", tb => tb.HasComment("角色表"));

            entity.Property(e => e.RoleId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("角色ID")
                .HasColumnName("RoleID");
            entity.Property(e => e.CreateTime)
                .HasComment("创建时间")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDel).HasComment("是否删除（false=未删除，true=已删除）");
            entity.Property(e => e.Memo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("备注");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("角色名称");
            entity.Property(e => e.UpdateTime)
                .HasComment("更新时间")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CCACC1CB6428");

            entity.ToTable("User", tb => tb.HasComment("用户表"));

            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("用户ID")
                .HasColumnName("UserID");
            entity.Property(e => e.CreateTime)
                .HasComment("创建时间")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDel).HasComment("是否删除（false=未删除，true=已删除）");
            entity.Property(e => e.Memo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("备注");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("用户密码");
            entity.Property(e => e.UpdateTime)
                .HasComment("更新时间")
                .HasColumnType("datetime");
            entity.Property(e => e.UserCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("工号");
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("用户姓名");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.UserRoleId).HasName("PK__UserRole__3D978A55E6F52B17");

            entity.ToTable("UserRole", tb => tb.HasComment("用户角色表"));

            entity.Property(e => e.UserRoleId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("用户角色ID")
                .HasColumnName("UserRoleID");
            entity.Property(e => e.RoleId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("角色ID（外键，关联Role表）")
                .HasColumnName("RoleID");
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("用户ID（外键，关联User表）")
                .HasColumnName("UserID");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRole_Role");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRole_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
