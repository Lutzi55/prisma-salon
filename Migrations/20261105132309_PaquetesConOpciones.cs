using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionEventos.Migrations
{
    /// <inheritdoc />
    public partial class PaquetesConOpciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Paquetes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CoordinadorIdCoordinador",
                table: "Paquetes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CapacidadInvitados",
                table: "Paquetes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DuracionHoras",
                table: "Paquetes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Incluye",
                table: "Paquetes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoriasOpcion",
                columns: table => new
                {
                    IdCategoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EsMultiple = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasOpcion", x => x.IdCategoria);
                });

            migrationBuilder.CreateTable(
                name: "Opciones",
                columns: table => new
                {
                    IdOpcion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCategoria = table.Column<int>(type: "int", nullable: false),
                    CategoriaIdCategoria = table.Column<int>(type: "int", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PrecioAdicional = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Colores = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opciones", x => x.IdOpcion);
                    table.ForeignKey(
                        name: "FK_Opciones_CategoriasOpcion_CategoriaIdCategoria",
                        column: x => x.CategoriaIdCategoria,
                        principalTable: "CategoriasOpcion",
                        principalColumn: "IdCategoria",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContratosOpciones",
                columns: table => new
                {
                    IdContrato = table.Column<int>(type: "int", nullable: false),
                    IdOpcion = table.Column<int>(type: "int", nullable: false),
                    ContratoIdContrato = table.Column<int>(type: "int", nullable: false),
                    OpcionIdOpcion = table.Column<int>(type: "int", nullable: false),
                    PrecioAcordado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosOpciones", x => new { x.IdContrato, x.IdOpcion });
                    table.ForeignKey(
                        name: "FK_ContratosOpciones_Contratos_ContratoIdContrato",
                        column: x => x.ContratoIdContrato,
                        principalTable: "Contratos",
                        principalColumn: "IdContrato",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContratosOpciones_Opciones_OpcionIdOpcion",
                        column: x => x.OpcionIdOpcion,
                        principalTable: "Opciones",
                        principalColumn: "IdOpcion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContratosOpciones_ContratoIdContrato",
                table: "ContratosOpciones",
                column: "ContratoIdContrato");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosOpciones_OpcionIdOpcion",
                table: "ContratosOpciones",
                column: "OpcionIdOpcion");

            migrationBuilder.CreateIndex(
                name: "IX_Opciones_CategoriaIdCategoria",
                table: "Opciones",
                column: "CategoriaIdCategoria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContratosOpciones");

            migrationBuilder.DropTable(
                name: "Opciones");

            migrationBuilder.DropTable(
                name: "CategoriasOpcion");

            migrationBuilder.DropColumn(
                name: "CapacidadInvitados",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "DuracionHoras",
                table: "Paquetes");

            migrationBuilder.DropColumn(
                name: "Incluye",
                table: "Paquetes");

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Paquetes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CoordinadorIdCoordinador",
                table: "Paquetes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
