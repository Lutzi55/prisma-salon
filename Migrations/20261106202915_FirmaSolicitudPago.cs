using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionEventos.Migrations
{
    /// <inheritdoc />
    public partial class FirmaSolicitudPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "Pagos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmaHash",
                table: "Contratos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirmadoEl",
                table: "Contratos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmanteNombre",
                table: "Contratos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutaFirmaCliente",
                table: "Contratos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "FirmaHash",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "FirmadoEl",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "FirmanteNombre",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "RutaFirmaCliente",
                table: "Contratos");
        }
    }
}
