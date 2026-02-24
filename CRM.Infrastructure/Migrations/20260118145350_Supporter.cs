using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Supporter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_SuperAdmins_AssignedTo",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_SuperAdmins_ClosedBy",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedByUserId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Supporter",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsFirstLogin = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supporter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Supporter_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_RejectedByUserId",
                table: "Tickets",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Supporter_RoleId",
                table: "Supporter",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Supporter_AssignedTo",
                table: "Tickets",
                column: "AssignedTo",
                principalTable: "Supporter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Supporter_ClosedBy",
                table: "Tickets",
                column: "ClosedBy",
                principalTable: "Supporter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Supporter_RejectedByUserId",
                table: "Tickets",
                column: "RejectedByUserId",
                principalTable: "Supporter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Supporter_AssignedTo",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Supporter_ClosedBy",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Supporter_RejectedByUserId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "Supporter");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_RejectedByUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "Tickets");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_SuperAdmins_AssignedTo",
                table: "Tickets",
                column: "AssignedTo",
                principalTable: "SuperAdmins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_SuperAdmins_ClosedBy",
                table: "Tickets",
                column: "ClosedBy",
                principalTable: "SuperAdmins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
