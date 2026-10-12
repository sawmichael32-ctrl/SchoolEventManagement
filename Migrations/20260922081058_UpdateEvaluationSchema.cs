using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolEventManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEvaluationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comments",
                table: "Evaluations");

            migrationBuilder.RenameColumn(
                name: "Rating",
                table: "Evaluations",
                newName: "VenueRating");

            migrationBuilder.AddColumn<int>(
                name: "ContentQuality",
                table: "Evaluations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "KeyTakeaways",
                table: "Evaluations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverallRating",
                table: "Evaluations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpeakerRating",
                table: "Evaluations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Suggestions",
                table: "Evaluations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentQuality",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "KeyTakeaways",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "OverallRating",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "SpeakerRating",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "Suggestions",
                table: "Evaluations");

            migrationBuilder.RenameColumn(
                name: "VenueRating",
                table: "Evaluations",
                newName: "Rating");

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "Evaluations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
