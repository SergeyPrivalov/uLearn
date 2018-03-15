using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace AntiPlagiarism.Web.Migrations
{
    public partial class AddSubmissionsCountToTaskStatisticsParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubmissionsCount",
                schema: "antiplagiarism",
                table: "TasksStatisticsParameters",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionsCount",
                schema: "antiplagiarism",
                table: "TasksStatisticsParameters");
        }
    }
}
