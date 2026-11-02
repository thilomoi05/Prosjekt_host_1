using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beredskapsportal.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilForesporsler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Foresporsler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BehovId = table.Column<int>(type: "int", nullable: false),
                    RessursId = table.Column<int>(type: "int", nullable: false),
                    ForespurtAv = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tidspunkt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Kommentar = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Begrunnelse = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BehandletAv = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foresporsler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Foresporsler_Behov_BehovId",
                        column: x => x.BehovId,
                        principalTable: "Behov",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Foresporsler_Ressurser_RessursId",
                        column: x => x.RessursId,
                        principalTable: "Ressurser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Foresporsler_BehovId",
                table: "Foresporsler",
                column: "BehovId");

            migrationBuilder.CreateIndex(
                name: "IX_Foresporsler_RessursId",
                table: "Foresporsler",
                column: "RessursId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Foresporsler");
        }
    }
}
