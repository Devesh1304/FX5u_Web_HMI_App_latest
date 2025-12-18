using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FX5u_Web_HMI_App.Migrations
{
    /// <inheritdoc />
    public partial class Add_NameTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NameTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    En = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Gu = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameTranslations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NameTranslations_En",
                table: "NameTranslations",
                column: "En",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NameTranslations");
        }
    }
}
