using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionEventos.Migrations
{
    /// <inheritdoc />
    public partial class MejorasContratos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Precio",
                table: "Shows");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRespuesta",
                table: "Resenas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Respuesta",
                table: "Resenas",
                type: "nvarchar(800)",
                maxLength: 800,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotaCliente",
                table: "Contratos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PedidoPorCliente",
                table: "Contratos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaRespuesta",
                table: "Resenas");

            migrationBuilder.DropColumn(
                name: "Respuesta",
                table: "Resenas");

            migrationBuilder.DropColumn(
                name: "NotaCliente",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "PedidoPorCliente",
                table: "Contratos");

            migrationBuilder.AddColumn<decimal>(
                name: "Precio",
                table: "Shows",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
