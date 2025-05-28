using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cloud9._2.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationStatusTableconcact1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_CommunicationTypes_CommunicationTypeId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Contacts_ContactId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Leads_LeadId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Orders_OrderId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Partners_PartnerId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Quotes_QuoteId",
                table: "CustomerCommunications");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "CustomerCommunications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ContactId",
                table: "CustomerCommunications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgentId",
                table: "CustomerCommunications",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "CustomerCommunications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeadId1",
                table: "CustomerCommunications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "CustomerCommunications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "CustomerCommunications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PartnerId1",
                table: "CustomerCommunications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuoteId1",
                table: "CustomerCommunications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "CustomerCommunications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Contacts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Contacts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CommunicationTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "CommunicationStatuses",
                columns: table => new
                {
                    StatusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationStatuses", x => x.StatusId);
                });

            migrationBuilder.InsertData(
                table: "CommunicationStatuses",
                columns: new[] { "StatusId", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Issue reported", "Open" },
                    { 2, "Being handled", "InProgress" },
                    { 3, "Issue closed", "Resolved" },
                    { 4, "Issue escalated to supervisor", "Escalated" }
                });

            migrationBuilder.InsertData(
                table: "CommunicationTypes",
                columns: new[] { "CommunicationTypeId", "Name" },
                values: new object[,]
                {
                    { 1, "Phone" },
                    { 2, "Email" },
                    { 3, "Meeting" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunications_AgentId",
                table: "CustomerCommunications",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunications_LeadId1",
                table: "CustomerCommunications",
                column: "LeadId1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunications_OrderId1",
                table: "CustomerCommunications",
                column: "OrderId1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunications_PartnerId1",
                table: "CustomerCommunications",
                column: "PartnerId1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunications_QuoteId1",
                table: "CustomerCommunications",
                column: "QuoteId1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunications_StatusId",
                table: "CustomerCommunications",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTypes_Name",
                table: "CommunicationTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationStatuses_Name",
                table: "CommunicationStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_AspNetUsers_AgentId",
                table: "CustomerCommunications",
                column: "AgentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_CommunicationStatuses_StatusId",
                table: "CustomerCommunications",
                column: "StatusId",
                principalTable: "CommunicationStatuses",
                principalColumn: "StatusId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_CommunicationTypes_CommunicationTypeId",
                table: "CustomerCommunications",
                column: "CommunicationTypeId",
                principalTable: "CommunicationTypes",
                principalColumn: "CommunicationTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Contacts_ContactId",
                table: "CustomerCommunications",
                column: "ContactId",
                principalTable: "Contacts",
                principalColumn: "ContactId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Leads_LeadId",
                table: "CustomerCommunications",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "LeadId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Leads_LeadId1",
                table: "CustomerCommunications",
                column: "LeadId1",
                principalTable: "Leads",
                principalColumn: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Orders_OrderId",
                table: "CustomerCommunications",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Orders_OrderId1",
                table: "CustomerCommunications",
                column: "OrderId1",
                principalTable: "Orders",
                principalColumn: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Partners_PartnerId",
                table: "CustomerCommunications",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Partners_PartnerId1",
                table: "CustomerCommunications",
                column: "PartnerId1",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Quotes_QuoteId",
                table: "CustomerCommunications",
                column: "QuoteId",
                principalTable: "Quotes",
                principalColumn: "QuoteId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Quotes_QuoteId1",
                table: "CustomerCommunications",
                column: "QuoteId1",
                principalTable: "Quotes",
                principalColumn: "QuoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_AspNetUsers_AgentId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_CommunicationStatuses_StatusId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_CommunicationTypes_CommunicationTypeId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Contacts_ContactId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Leads_LeadId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Leads_LeadId1",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Orders_OrderId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Orders_OrderId1",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Partners_PartnerId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Partners_PartnerId1",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Quotes_QuoteId",
                table: "CustomerCommunications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerCommunications_Quotes_QuoteId1",
                table: "CustomerCommunications");

            migrationBuilder.DropTable(
                name: "CommunicationStatuses");

            migrationBuilder.DropIndex(
                name: "IX_CustomerCommunications_AgentId",
                table: "CustomerCommunications");

            migrationBuilder.DropIndex(
                name: "IX_CustomerCommunications_LeadId1",
                table: "CustomerCommunications");

            migrationBuilder.DropIndex(
                name: "IX_CustomerCommunications_OrderId1",
                table: "CustomerCommunications");

            migrationBuilder.DropIndex(
                name: "IX_CustomerCommunications_PartnerId1",
                table: "CustomerCommunications");

            migrationBuilder.DropIndex(
                name: "IX_CustomerCommunications_QuoteId1",
                table: "CustomerCommunications");

            migrationBuilder.DropIndex(
                name: "IX_CustomerCommunications_StatusId",
                table: "CustomerCommunications");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationTypes_Name",
                table: "CommunicationTypes");

            migrationBuilder.DeleteData(
                table: "CommunicationTypes",
                keyColumn: "CommunicationTypeId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CommunicationTypes",
                keyColumn: "CommunicationTypeId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CommunicationTypes",
                keyColumn: "CommunicationTypeId",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "CustomerCommunications");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "CustomerCommunications");

            migrationBuilder.DropColumn(
                name: "LeadId1",
                table: "CustomerCommunications");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "CustomerCommunications");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "CustomerCommunications");

            migrationBuilder.DropColumn(
                name: "PartnerId1",
                table: "CustomerCommunications");

            migrationBuilder.DropColumn(
                name: "QuoteId1",
                table: "CustomerCommunications");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "CustomerCommunications");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "CustomerCommunications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ContactId",
                table: "CustomerCommunications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Contacts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Contacts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CommunicationTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_CommunicationTypes_CommunicationTypeId",
                table: "CustomerCommunications",
                column: "CommunicationTypeId",
                principalTable: "CommunicationTypes",
                principalColumn: "CommunicationTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Contacts_ContactId",
                table: "CustomerCommunications",
                column: "ContactId",
                principalTable: "Contacts",
                principalColumn: "ContactId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Leads_LeadId",
                table: "CustomerCommunications",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Orders_OrderId",
                table: "CustomerCommunications",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Partners_PartnerId",
                table: "CustomerCommunications",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerCommunications_Quotes_QuoteId",
                table: "CustomerCommunications",
                column: "QuoteId",
                principalTable: "Quotes",
                principalColumn: "QuoteId");
        }
    }
}
