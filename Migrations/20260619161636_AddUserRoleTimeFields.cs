using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleTimeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // User 表新增时间字段
            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "User",
                type: "datetime2",
                nullable: true);

            // Role 表新增时间字段（IsDel 已存在于数据库中）
            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Role",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "Role",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "User");

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "Role");
        }
    }
}
