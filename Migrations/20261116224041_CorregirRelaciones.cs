using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionEventos.Migrations
{
    /// <inheritdoc />
    public partial class CorregirRelaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Clientes_ClienteIdCliente",
                table: "Contratos");

            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Paquetes_PaqueteIdPaquete",
                table: "Contratos");

            migrationBuilder.DropForeignKey(
                name: "FK_Opciones_CategoriasOpcion_CategoriaIdCategoria",
                table: "Opciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Contratos_ContratoIdContrato",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_Paquetes_Coordinadores_CoordinadorIdCoordinador",
                table: "Paquetes");

            migrationBuilder.DropIndex(
                name: "IX_Paquetes_CoordinadorIdCoordinador",
                table: "Paquetes");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_ContratoIdContrato",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Opciones_CategoriaIdCategoria",
                table: "Opciones");

            migrationBuilder.DropIndex(
                name: "IX_Contratos_ClienteIdCliente",
                table: "Contratos");

            migrationBuilder.DropIndex(
                name: "IX_Contratos_PaqueteIdPaquete",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "CoordinadorIdCoordinador",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "ContratoIdContrato",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "CategoriaIdCategoria",
                table: "Opciones");

            migrationBuilder.DropColumn(
                name: "ClienteIdCliente",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "PaqueteIdPaquete",
                table: "Contratos");

            migrationBuilder.CreateIndex(
                name: "IX_Paquetes_IdCoordinador",
                table: "Paquetes",
                column: "IdCoordinador");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdContrato",
                table: "Pagos",
                column: "IdContrato");

            migrationBuilder.CreateIndex(
                name: "IX_Opciones_IdCategoria",
                table: "Opciones",
                column: "IdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_IdCliente",
                table: "Contratos",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_IdPaquete",
                table: "Contratos",
                column: "IdPaquete");

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Clientes_IdCliente",
                table: "Contratos",
                column: "IdCliente",
                principalTable: "Clientes",
                principalColumn: "IdCliente",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Paquetes_IdPaquete",
                table: "Contratos",
                column: "IdPaquete",
                principalTable: "Paquetes",
                principalColumn: "IdPaquete",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Opciones_CategoriasOpcion_IdCategoria",
                table: "Opciones",
                column: "IdCategoria",
                principalTable: "CategoriasOpcion",
                principalColumn: "IdCategoria",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Contratos_IdContrato",
                table: "Pagos",
                column: "IdContrato",
                principalTable: "Contratos",
                principalColumn: "IdContrato",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Paquetes_Coordinadores_IdCoordinador",
                table: "Paquetes",
                column: "IdCoordinador",
                principalTable: "Coordinadores",
                principalColumn: "IdCoordinador",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Clientes_IdCliente",
                table: "Contratos");

            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Paquetes_IdPaquete",
                table: "Contratos");

            migrationBuilder.DropForeignKey(
                name: "FK_Opciones_CategoriasOpcion_IdCategoria",
                table: "Opciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Contratos_IdContrato",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_Paquetes_Coordinadores_IdCoordinador",
                table: "Paquetes");

            migrationBuilder.DropIndex(
                name: "IX_Paquetes_IdCoordinador",
                table: "Paquetes");

            migrationBuilder.DropIndex(
                name: "IX_Pagos_IdContrato",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Opciones_IdCategoria",
                table: "Opciones");

            migrationBuilder.DropIndex(
                name: "IX_Contratos_IdCliente",
                table: "Contratos");

            migrationBuilder.DropIndex(
                name: "IX_Contratos_IdPaquete",
                table: "Contratos");

            migrationBuilder.AddColumn<int>(
                name: "CoordinadorIdCoordinador",
                table: "Paquetes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContratoIdContrato",
                table: "Pagos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CategoriaIdCategoria",
                table: "Opciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClienteIdCliente",
                table: "Contratos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaqueteIdPaquete",
                table: "Contratos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Paquetes_CoordinadorIdCoordinador",
                table: "Paquetes",
                column: "CoordinadorIdCoordinador");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ContratoIdContrato",
                table: "Pagos",
                column: "ContratoIdContrato");

            migrationBuilder.CreateIndex(
                name: "IX_Opciones_CategoriaIdCategoria",
                table: "Opciones",
                column: "CategoriaIdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_ClienteIdCliente",
                table: "Contratos",
                column: "ClienteIdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_PaqueteIdPaquete",
                table: "Contratos",
                column: "PaqueteIdPaquete");

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Clientes_ClienteIdCliente",
                table: "Contratos",
                column: "ClienteIdCliente",
                principalTable: "Clientes",
                principalColumn: "IdCliente",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Paquetes_PaqueteIdPaquete",
                table: "Contratos",
                column: "PaqueteIdPaquete",
                principalTable: "Paquetes",
                principalColumn: "IdPaquete",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Opciones_CategoriasOpcion_CategoriaIdCategoria",
                table: "Opciones",
                column: "CategoriaIdCategoria",
                principalTable: "CategoriasOpcion",
                principalColumn: "IdCategoria",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Contratos_ContratoIdContrato",
                table: "Pagos",
                column: "ContratoIdContrato",
                principalTable: "Contratos",
                principalColumn: "IdContrato",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Paquetes_Coordinadores_CoordinadorIdCoordinador",
                table: "Paquetes",
                column: "CoordinadorIdCoordinador",
                principalTable: "Coordinadores",
                principalColumn: "IdCoordinador",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
