using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace AntiPlagiarism.Web.Migrations
{
    public partial class AddUniqueTokenAndEnabledForClients : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Clients_Token_IsEnabled",
                schema: "antiplagiarism",
                table: "Clients",
                columns: new[] { "Token", "IsEnabled" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_Token_IsEnabled",
                schema: "antiplagiarism",
                table: "Clients");
        }
    }
}
