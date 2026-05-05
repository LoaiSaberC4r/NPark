using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterPricingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnterPriceSchema",
                table: "PricingSchemes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_PricingSchemes_IsEnterPriceSchema",
                table: "PricingSchemes",
                column: "IsEnterPriceSchema");

            migrationBuilder.CreateIndex(
                name: "IX_PricingSchemes_IsRepeated",
                table: "PricingSchemes",
                column: "IsRepeated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PricingSchemes_IsEnterPriceSchema",
                table: "PricingSchemes");

            migrationBuilder.DropIndex(
                name: "IX_PricingSchemes_IsRepeated",
                table: "PricingSchemes");

            migrationBuilder.DropColumn(
                name: "IsEnterPriceSchema",
                table: "PricingSchemes");
        }
    }
}
