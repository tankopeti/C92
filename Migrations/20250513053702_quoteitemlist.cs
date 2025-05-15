using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloud9._2.Migrations
{
    /// <inheritdoc />
    public partial class quoteitemlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "QuoteItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_ProductId1",
                table: "QuoteItems",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteItems_Products_ProductId1",
                table: "QuoteItems",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteItems_Products_ProductId1",
                table: "QuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItems_ProductId1",
                table: "QuoteItems");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "QuoteItems");
        }
    }
}
