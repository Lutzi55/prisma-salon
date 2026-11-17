using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionEventos.Migrations
{
    /// <inheritdoc />
    public partial class GestionPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimientoOriginal",
                table: "Pagos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nota",
                table: "Pagos",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaVencimientoOriginal",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "Nota",
                table: "Pagos");
        }
    }
}
