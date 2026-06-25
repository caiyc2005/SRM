using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========== 创建供应商用户关联表 ==========
            // 注意：SupplierID / UserID 在现有数据库中是 varchar(50)，需与外键类型匹配
            migrationBuilder.Sql(@"
                CREATE TABLE [SupplierUser] (
                    [SupplierUserID] nvarchar(50) NOT NULL,
                    [SupplierID] varchar(50) NOT NULL,
                    [UserID] varchar(50) NOT NULL,
                    [IsMainAccount] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_SupplierUser] PRIMARY KEY ([SupplierUserID]),
                    CONSTRAINT [FK_SupplierUser_Supplier_SupplierID] FOREIGN KEY ([SupplierID]) REFERENCES [Supplier] ([SupplierID]),
                    CONSTRAINT [FK_SupplierUser_User_UserID] FOREIGN KEY ([UserID]) REFERENCES [User] ([UserID])
                );
            ");

            // ========== 将现有 Supplier.UserID 数据迁移到 SupplierUser 表（设为主账号） ==========
            migrationBuilder.Sql(@"
                INSERT INTO [SupplierUser] ([SupplierUserID], [SupplierID], [UserID], [IsMainAccount], [CreatedAt])
                SELECT NEWID(), [SupplierID], [UserID], 1, GETDATE()
                FROM [Supplier]
                WHERE [UserID] IS NOT NULL
            ");

            // ========== SupplierUser 索引 ==========
            migrationBuilder.CreateIndex(
                name: "IX_SupplierUser_SupplierID_UserID",
                table: "SupplierUser",
                columns: new[] { "SupplierID", "UserID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierUser_UserID",
                table: "SupplierUser",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ========== 删除 SupplierUser 相关索引 ==========
            // 注意：DropTable 会自动删除索引，但这里只删除 SupplierUser 相关内容
            migrationBuilder.Sql(@"
                DELETE FROM [SupplierUser]
            ");

            // ========== 删除供应商用户关联表 ==========
            migrationBuilder.DropTable(
                name: "SupplierUser");
        }
    }
}
