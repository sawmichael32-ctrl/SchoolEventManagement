using Microsoft.EntityFrameworkCore.Migrations;
using SchoolEventManagement.Data;

#nullable disable

namespace SchoolEventManagement.Migrations
{
    public partial class AddSchoolLevelsAndEventAudience : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "SchoolLevel", table: "Registrations", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "College");
            migrationBuilder.AddColumn<string>(name: "GradeLevel", table: "Registrations", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "TargetSchoolLevel", table: "Events", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "All");
            migrationBuilder.AddColumn<string>(name: "TargetGradeLevel", table: "Events", type: "nvarchar(20)", maxLength: 20, nullable: true);
            migrationBuilder.AddColumn<string>(name: "TargetProgram", table: "Events", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "TargetSection", table: "Events", type: "nvarchar(50)", maxLength: 50, nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SchoolLevel", table: "Registrations");
            migrationBuilder.DropColumn(name: "GradeLevel", table: "Registrations");
            migrationBuilder.DropColumn(name: "TargetSchoolLevel", table: "Events");
            migrationBuilder.DropColumn(name: "TargetGradeLevel", table: "Events");
            migrationBuilder.DropColumn(name: "TargetProgram", table: "Events");
            migrationBuilder.DropColumn(name: "TargetSection", table: "Events");
        }
    }
}
