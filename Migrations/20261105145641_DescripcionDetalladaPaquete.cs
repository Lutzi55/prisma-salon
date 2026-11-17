using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionEventos.Migrations
{
    /// <inheritdoc />
    public partial class DescripcionDetalladaPaquete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescripcionDetallada",
                table: "Paquetes",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescripcionDetallada",
                table: "Paquetes");
        }
    }
}
