using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevHabit.Api.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddUserIdReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM dev_habit.habit_tags;
                DELETE FROM dev_habit.habits;
                DELETE FROM dev_habit.tags;
            """);

            migrationBuilder.DropIndex(
                name: "ix_tags_name",
                schema: "dev_habit",
                table: "tags");

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                schema: "dev_habit",
                table: "tags",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                schema: "dev_habit",
                table: "habits",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_tags_user_id_name",
                schema: "dev_habit",
                table: "tags",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_habits_user_id",
                schema: "dev_habit",
                table: "habits",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_habits_users_user_id",
                schema: "dev_habit",
                table: "habits",
                column: "user_id",
                principalSchema: "dev_habit",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tags_users_user_id",
                schema: "dev_habit",
                table: "tags",
                column: "user_id",
                principalSchema: "dev_habit",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_habits_users_user_id",
                schema: "dev_habit",
                table: "habits");

            migrationBuilder.DropForeignKey(
                name: "fk_tags_users_user_id",
                schema: "dev_habit",
                table: "tags");

            migrationBuilder.DropIndex(
                name: "ix_tags_user_id_name",
                schema: "dev_habit",
                table: "tags");

            migrationBuilder.DropIndex(
                name: "ix_habits_user_id",
                schema: "dev_habit",
                table: "habits");

            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "dev_habit",
                table: "tags");

            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "dev_habit",
                table: "habits");

            migrationBuilder.CreateIndex(
                name: "ix_tags_name",
                schema: "dev_habit",
                table: "tags",
                column: "name",
                unique: true);
        }
    }
}
