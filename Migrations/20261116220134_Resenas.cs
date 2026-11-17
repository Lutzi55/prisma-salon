using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionEventos.Migrations
{
    /// <inheritdoc />
    public partial class Resenas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContratosOpciones_Contratos_ContratoIdContrato",
                table: "ContratosOpciones");

            migrationBuilder.DropForeignKey(
                name: "FK_ContratosOpciones_Opciones_OpcionIdOpcion",
                table: "ContratosOpciones");

            migrationBuilder.DropIndex(
                name: "IX_ContratosOpciones_ContratoIdContrato",
                table: "ContratosOpciones");

            migrationBuilder.DropIndex(
                name: "IX_ContratosOpciones_OpcionIdOpcion",
                table: "ContratosOpciones");

            migrationBuilder.DropColumn(
                name: "ContratoIdContrato",
                table: "ContratosOpciones");

            migrationBuilder.DropColumn(
                name: "OpcionIdOpcion",
                table: "ContratosOpciones");

            migrationBuilder.CreateTable(
                name: "Resenas",
                columns: table => new
                {
                    IdResena = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdContrato = table.Column<int>(type: "int", nullable: false),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    Puntaje = table.Column<int>(type: "int", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Visible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resenas", x => x.IdResena);
                    table.ForeignKey(
                        name: "FK_Resenas_Clientes_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clientes",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resenas_Contratos_IdContrato",
                        column: x => x.IdContrato,
                        principalTable: "Contratos",
                        principalColumn: "IdContrato",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContratosOpciones_IdOpcion",
                table: "ContratosOpciones",
                column: "IdOpcion");

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_IdCliente",
                table: "Resenas",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_IdContrato",
                table: "Resenas",
                column: "IdContrato",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContratosOpciones_Contratos_IdContrato",
                table: "ContratosOpciones",
                column: "IdContrato",
                principalTable: "Contratos",
                principalColumn: "IdContrato",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContratosOpciones_Opciones_IdOpcion",
                table: "ContratosOpciones",
                column: "IdOpcion",
                principalTable: "Opciones",
                principalColumn: "IdOpcion",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContratosOpciones_Contratos_IdContrato",
                table: "ContratosOpciones");

            migrationBuilder.DropForeignKey(
                name: "FK_ContratosOpciones_Opciones_IdOpcion",
                table: "ContratosOpciones");

            migrationBuilder.DropTable(
                name: "Resenas");

            migrationBuilder.DropIndex(
                name: "IX_ContratosOpciones_IdOpcion",
                table: "ContratosOpciones");

            migrationBuilder.AddColumn<int>(
                name: "ContratoIdContrato",
                table: "ContratosOpciones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpcionIdOpcion",
                table: "ContratosOpciones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ContratosOpciones_ContratoIdContrato",
                table: "ContratosOpciones",
                column: "ContratoIdContrato");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosOpciones_OpcionIdOpcion",
                table: "ContratosOpciones",
                column: "OpcionIdOpcion");

            migrationBuilder.AddForeignKey(
                name: "FK_ContratosOpciones_Contratos_ContratoIdContrato",
                table: "ContratosOpciones",
                column: "ContratoIdContrato",
                principalTable: "Contratos",
                principalColumn: "IdContrato",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContratosOpciones_Opciones_OpcionIdOpcion",
                table: "ContratosOpciones",
                column: "OpcionIdOpcion",
                principalTable: "Opciones",
                principalColumn: "IdOpcion",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
