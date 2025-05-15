using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloud9._2.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteHistoryToQuoteModify2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangeType",
                table: "QuoteHistories");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "QuoteHistories");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "QuoteHistories");

            migrationBuilder.RenameColumn(
                name: "QuoteNumber",
                table: "QuoteHistories",
                newName: "ModifiedBy");

            migrationBuilder.RenameColumn(
                name: "ChangedBy",
                table: "QuoteHistories",
                newName: "FieldName");

            migrationBuilder.RenameColumn(
                name: "ChangeDate",
                table: "QuoteHistories",
                newName: "ModifiedDate");

            migrationBuilder.AddColumn<string>(
                name: "NewValue",
                table: "QuoteHistories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldValue",
                table: "QuoteHistories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewValue",
                table: "QuoteHistories");

            migrationBuilder.DropColumn(
                name: "OldValue",
                table: "QuoteHistories");

            migrationBuilder.RenameColumn(
                name: "ModifiedDate",
                table: "QuoteHistories",
                newName: "ChangeDate");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "QuoteHistories",
                newName: "QuoteNumber");

            migrationBuilder.RenameColumn(
                name: "FieldName",
                table: "QuoteHistories",
                newName: "ChangedBy");

            migrationBuilder.AddColumn<string>(
                name: "ChangeType",
                table: "QuoteHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PartnerId",
                table: "QuoteHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "QuoteHistories",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
