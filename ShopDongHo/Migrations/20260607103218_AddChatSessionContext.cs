using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopDongHo.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSessionContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatSessionContexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastBrand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastProductId = table.Column<long>(type: "bigint", nullable: true),
                    LastCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessionContexts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatSessionContexts");
        }
    }
}
