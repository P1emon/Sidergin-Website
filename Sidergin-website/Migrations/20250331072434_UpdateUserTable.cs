using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sidergin_website.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Đổi tên cột "role" thành "name"
            migrationBuilder.RenameColumn(
                name: "role",
                table: "users",
                newName: "name");

            // Cập nhật độ dài của name thành 100 ký tự
            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            // Cập nhật phone để có thể null
            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "users",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);
        }
    }
}
