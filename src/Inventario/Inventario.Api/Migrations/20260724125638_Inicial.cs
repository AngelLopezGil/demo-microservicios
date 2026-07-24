using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Inventario.Api.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Disponible = table.Column<int>(type: "int", nullable: false),
                    Reservado = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.ProductoId);
                });

            migrationBuilder.InsertData(
                table: "Stocks",
                columns: new[] { "ProductoId", "Disponible", "Reservado" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333333"), 10, 0 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 3, 0 },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 0, 0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stocks");
        }
    }
}
