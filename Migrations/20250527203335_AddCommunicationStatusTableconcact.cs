using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloud9._2.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationStatusTableconcact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContactId",
                table: "CustomerCommunications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunications_ContactId",
                table: "CustomerCommunications",
                column: "ContactId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Contacts_ContactId",
                table: "CustomerCommunications",
                column: "ContactId",
                principalTable: "Contacts",
                principalColumn: "ContactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Contacts_ContactId",
                table: "CustomerCommunications");

            migrationBuilder.DropIndex(
                name: "IX_CustomerCommunications_ContactId",
                table: "CustomerCommunications");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "CustomerCommunications");
        }
    }
}
