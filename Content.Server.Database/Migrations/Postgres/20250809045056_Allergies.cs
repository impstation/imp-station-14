using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Allergies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cd_character_allergies",
                columns: table => new
                {
                    allergen = table.Column<string>(type: "text", nullable: false),
                    cdprofile_id = table.Column<int>(type: "integer", nullable: false),
                    intensity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cd_character_allergies", x => new { x.cdprofile_id, x.allergen });
                    table.ForeignKey(
                        name: "FK_cd_character_allergies_cdprofile_cdprofile_id",
                        column: x => x.cdprofile_id,
                        principalTable: "cdprofile",
                        principalColumn: "cdprofile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cd_character_allergies_cdprofile_id_allergen",
                table: "cd_character_allergies",
                columns: new[] { "cdprofile_id", "allergen" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cd_character_allergies");
        }
    }
}
