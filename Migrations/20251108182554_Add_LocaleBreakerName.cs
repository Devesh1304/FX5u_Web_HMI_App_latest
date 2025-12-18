using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FX5u_Web_HMI_App.Migrations
{
    /// <inheritdoc />
    public partial class Add_LocaleBreakerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Torque = table.Column<double>(type: "REAL", nullable: false),
                    Position = table.Column<double>(type: "REAL", nullable: false),
                    RPM = table.Column<double>(type: "REAL", nullable: false),
                    BrakerNo = table.Column<int>(type: "INTEGER", nullable: false),
                    BreakerDescription = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocaleBreakerNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Lang = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocaleBreakerNames", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocaleBreakerNames_Id_Lang",
                table: "LocaleBreakerNames",
                columns: new[] { "Id", "Lang" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataLogs");

            migrationBuilder.DropTable(
                name: "LocaleBreakerNames");
        }
    }
}
