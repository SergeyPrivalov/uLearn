using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Database.Migrations
{
    public partial class AddAntiPlagiarismSubmissionId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AntiPlagiarismSubmissionId",
                table: "UserExerciseSubmissions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExerciseSubmissions_AntiPlagiarismSubmissionId",
                table: "UserExerciseSubmissions",
                column: "AntiPlagiarismSubmissionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserExerciseSubmissions_AntiPlagiarismSubmissionId",
                table: "UserExerciseSubmissions");

            migrationBuilder.DropColumn(
                name: "AntiPlagiarismSubmissionId",
                table: "UserExerciseSubmissions");
        }
    }
}
