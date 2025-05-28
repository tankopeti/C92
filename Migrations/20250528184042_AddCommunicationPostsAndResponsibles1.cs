using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloud9._2.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationPostsAndResponsibles1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunicationPosts",
                columns: table => new
                {
                    CommunicationPostId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerCommunicationId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationPosts", x => x.CommunicationPostId);
                    table.ForeignKey(
                        name: "FK_CommunicationPosts_Contacts_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Contacts",
                        principalColumn: "ContactId");
                    table.ForeignKey(
                        name: "FK_CommunicationPosts_CustomerCommunications_CustomerCommunicationId",
                        column: x => x.CustomerCommunicationId,
                        principalTable: "CustomerCommunications",
                        principalColumn: "CustomerCommunicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationResponsibles",
                columns: table => new
                {
                    CommunicationResponsibleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerCommunicationId = table.Column<int>(type: "int", nullable: false),
                    ResponsibleId = table.Column<int>(type: "int", nullable: true),
                    AssignedById = table.Column<int>(type: "int", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationResponsibles", x => x.CommunicationResponsibleId);
                    table.ForeignKey(
                        name: "FK_CommunicationResponsibles_Contacts_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "Contacts",
                        principalColumn: "ContactId");
                    table.ForeignKey(
                        name: "FK_CommunicationResponsibles_Contacts_ResponsibleId",
                        column: x => x.ResponsibleId,
                        principalTable: "Contacts",
                        principalColumn: "ContactId");
                    table.ForeignKey(
                        name: "FK_CommunicationResponsibles_CustomerCommunications_CustomerCommunicationId",
                        column: x => x.CustomerCommunicationId,
                        principalTable: "CustomerCommunications",
                        principalColumn: "CustomerCommunicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPosts_CreatedById",
                table: "CommunicationPosts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPosts_CustomerCommunicationId",
                table: "CommunicationPosts",
                column: "CustomerCommunicationId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationResponsibles_AssignedById",
                table: "CommunicationResponsibles",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationResponsibles_CustomerCommunicationId",
                table: "CommunicationResponsibles",
                column: "CustomerCommunicationId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationResponsibles_ResponsibleId",
                table: "CommunicationResponsibles",
                column: "ResponsibleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunicationPosts");

            migrationBuilder.DropTable(
                name: "CommunicationResponsibles");
        }
    }
}
