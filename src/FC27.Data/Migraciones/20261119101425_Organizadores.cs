using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FC27.Data.Migraciones
{
    /// <inheritdoc />
    public partial class Organizadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Torneos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizadorId",
                table: "Torneos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Organizadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Usuario = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Sal = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizadores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invitaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", nullable: false),
                    UsadaPorId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitaciones_Organizadores_UsadaPorId",
                        column: x => x.UsadaPorId,
                        principalTable: "Organizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Torneos_Codigo",
                table: "Torneos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Torneos_OrganizadorId",
                table: "Torneos",
                column: "OrganizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitaciones_Codigo",
                table: "Invitaciones",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitaciones_UsadaPorId",
                table: "Invitaciones",
                column: "UsadaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizadores_Usuario",
                table: "Organizadores",
                column: "Usuario",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Torneos_Organizadores_OrganizadorId",
                table: "Torneos",
                column: "OrganizadorId",
                principalTable: "Organizadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Torneos_Organizadores_OrganizadorId",
                table: "Torneos");

            migrationBuilder.DropTable(
                name: "Invitaciones");

            migrationBuilder.DropTable(
                name: "Organizadores");

            migrationBuilder.DropIndex(
                name: "IX_Torneos_Codigo",
                table: "Torneos");

            migrationBuilder.DropIndex(
                name: "IX_Torneos_OrganizadorId",
                table: "Torneos");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Torneos");

            migrationBuilder.DropColumn(
                name: "OrganizadorId",
                table: "Torneos");
        }
    }
}
