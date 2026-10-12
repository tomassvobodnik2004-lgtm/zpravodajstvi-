using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zpravodajstvi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sloupec i cizí klíč přidá jediný příkaz ALTER TABLE (SQLite to u sloupce s výchozí hodnotou NULL dovoluje).
            // Vygenerované AddColumn + AddForeignKey by v SQLite přestavovalo celou tabulku Comments mimo transakci.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Comments" ADD "UserId" TEXT NULL
                    CONSTRAINT "FK_Comments_AspNetUsers_UserId" REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL;
                """);

            // Doplnění autora u existujících komentářů podle shody jména s FullName uživatele.
            // UserId se nastaví jen při jednoznačné shodě (právě jeden uživatel s tímto jménem),
            // ostatní komentáře zůstanou s UserId = null a spravovat je může jen administrátor.
            migrationBuilder.Sql(
                """
                UPDATE "Comments"
                SET "UserId" = (
                    SELECT "u"."Id"
                    FROM "AspNetUsers" AS "u"
                    WHERE trim("u"."FullName") = trim("Comments"."AuthorName"))
                WHERE trim("AuthorName") <> ''
                    AND (
                        SELECT COUNT(*)
                        FROM "AspNetUsers" AS "u"
                        WHERE trim("u"."FullName") = trim("Comments"."AuthorName")) = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_AspNetUsers_UserId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_UserId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Comments");
        }
    }
}
