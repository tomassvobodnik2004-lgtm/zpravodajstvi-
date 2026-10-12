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

            // Doplnění normalizovaných hodnot pro existující články a štítky
            migrationBuilder.Sql(
                $"""
                UPDATE "Articles"
                SET "SearchTitle" = {NormalizeSql("Title")},
                    "SearchPerex" = {NormalizeSql("Perex")},
                    "SearchContent" = {NormalizeSql("Content")};
                """);

            migrationBuilder.Sql($"""UPDATE "Tags" SET "SearchName" = {NormalizeSql("Name")};""");
        }

        // Česká a slovenská písmena s diakritikou a jejich náhrada (malé písmeno bez diakritiky)
        private const string DiacriticChars = "áäčďéěíĺľňóôŕřšťúůýžÁÄČĎÉĚÍĹĽŇÓÔŔŘŠŤÚŮÝŽ";
        private const string PlainChars = "aacdeeillnoorrstuuyzaacdeeillnoorrstuuyz";

        // SQL výraz se stejným výsledkem jako SearchNormalizer.Normalize() pro český a slovenský text.
        // SQLite funkce lower() převádí jen písmena ASCII, proto se nejdřív nahradí znaky s diakritikou.
        private static string NormalizeSql(string column)
        {
            var expression = $"\"{column}\"";
            for (var i = 0; i < DiacriticChars.Length; i++)
            {
                expression = $"replace({expression}, '{DiacriticChars[i]}', '{PlainChars[i]}')";
            }

            return $"lower({expression})";
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
