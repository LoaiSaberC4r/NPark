using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddErollmentColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EnrollmentDate",
                table: "SiteProducts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteProducts_EnrollmentDate",
                table: "SiteProducts",
                column: "EnrollmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_SiteProducts_Id",
                table: "SiteProducts",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SiteProducts_EnrollmentDate",
                table: "SiteProducts");

            migrationBuilder.DropIndex(
                name: "IX_SiteProducts_Id",
                table: "SiteProducts");

            migrationBuilder.DropColumn(
                name: "EnrollmentDate",
                table: "SiteProducts");
        }
    }
}
