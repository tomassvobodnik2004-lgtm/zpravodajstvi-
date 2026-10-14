using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zpravodajstvi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchName",
                table: "Tags",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchContent",
                table: "Articles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchPerex",
                table: "Articles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchTitle",
                table: "Articles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Hodnoty Search* vyplňuje ApplicationDbContext při každém uložení článku nebo štítku.
            // Při vytváření nové databáze jsou tabulky v tomto kroku ještě prázdné.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchName",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "SearchContent",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SearchPerex",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SearchTitle",
                table: "Articles");
        }
    }
}
