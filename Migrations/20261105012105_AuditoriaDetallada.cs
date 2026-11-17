using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionEventos.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaDetallada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Auditorias",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Detalle",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "Detalle",
                table: "Auditorias");
        }
    }
}
