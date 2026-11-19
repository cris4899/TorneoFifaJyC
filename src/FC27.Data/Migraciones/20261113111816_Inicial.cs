using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FC27.Data.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Torneos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    NumeroParticipantes = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Torneos", x => x.Id);
                    table.CheckConstraint("CK_Torneo_Capacidad", "NumeroParticipantes IN (4, 8, 16, 32, 64)");
                    table.CheckConstraint("CK_Torneo_Estado", "Estado IN ('Borrador', 'EnCurso', 'Finalizado')");
                });

            migrationBuilder.CreateTable(
                name: "Fases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TorneoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fases", x => x.Id);
                    table.CheckConstraint("CK_Fase_Orden", "Orden >= 1");
                    table.ForeignKey(
                        name: "FK_Fases_Torneos_TorneoId",
                        column: x => x.TorneoId,
                        principalTable: "Torneos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Participantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TorneoId = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreJugador = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    EquipoFC27 = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participantes_Torneos_TorneoId",
                        column: x => x.TorneoId,
                        principalTable: "Torneos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Partidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FaseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Posicion = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipoLocalId = table.Column<int>(type: "INTEGER", nullable: true),
                    EquipoVisitanteId = table.Column<int>(type: "INTEGER", nullable: true),
                    GolesLocal = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    GolesVisitante = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    EstadoPartido = table.Column<string>(type: "TEXT", nullable: false),
                    GanadorId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidos", x => x.Id);
                    table.CheckConstraint("CK_Partido_Goles", "GolesLocal >= 0 AND GolesVisitante >= 0");
                    table.CheckConstraint("CK_Partido_Plazas", "EquipoLocalId <> EquipoVisitanteId");
                    table.CheckConstraint("CK_Partido_Posicion", "Posicion >= 1");
                    table.CheckConstraint("CK_Partido_Resultado", "(EstadoPartido = 'Pendiente' AND GolesLocal = 0 AND GolesVisitante = 0 AND GanadorId IS NULL)\nOR\n(EstadoPartido = 'Jugado' AND EquipoLocalId IS NOT NULL AND EquipoVisitanteId IS NOT NULL\n  AND GanadorId IS NOT NULL AND GanadorId IN (EquipoLocalId, EquipoVisitanteId)\n  AND (GolesLocal = GolesVisitante\n    OR (GolesLocal > GolesVisitante AND GanadorId = EquipoLocalId)\n    OR (GolesVisitante > GolesLocal AND GanadorId = EquipoVisitanteId)))");
                    table.ForeignKey(
                        name: "FK_Partidos_Fases_FaseId",
                        column: x => x.FaseId,
                        principalTable: "Fases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Partidos_Participantes_EquipoLocalId",
                        column: x => x.EquipoLocalId,
                        principalTable: "Participantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Partidos_Participantes_EquipoVisitanteId",
                        column: x => x.EquipoVisitanteId,
                        principalTable: "Participantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Partidos_Participantes_GanadorId",
                        column: x => x.GanadorId,
                        principalTable: "Participantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fases_TorneoId_Orden",
                table: "Fases",
                columns: new[] { "TorneoId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participantes_Email",
                table: "Participantes",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participantes_TorneoId",
                table: "Participantes",
                column: "TorneoId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_EquipoLocalId",
                table: "Partidos",
                column: "EquipoLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_EquipoVisitanteId",
                table: "Partidos",
                column: "EquipoVisitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_FaseId_Posicion",
                table: "Partidos",
                columns: new[] { "FaseId", "Posicion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_GanadorId",
                table: "Partidos",
                column: "GanadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Torneos_Nombre",
                table: "Torneos",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Partidos");

            migrationBuilder.DropTable(
                name: "Fases");

            migrationBuilder.DropTable(
                name: "Participantes");

            migrationBuilder.DropTable(
                name: "Torneos");
        }
    }
}
