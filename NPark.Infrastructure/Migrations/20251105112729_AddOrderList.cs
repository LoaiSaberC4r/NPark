using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderPricingSchemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PricingSchemaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPricingSchemas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPricingSchemas_PricingSchemes_PricingSchemaId",
                        column: x => x.PricingSchemaId,
                        principalTable: "PricingSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPricingSchemas_PricingSchemaId",
                table: "OrderPricingSchemas",
                column: "PricingSchemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPricingSchemas");
        }
    }
}
