using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloud9._2.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Sites",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedById",
                table: "Sites",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "Sites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_CreatedById",
                table: "Sites",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_LastModifiedById",
                table: "Sites",
                column: "LastModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_AspNetUsers_CreatedById",
                table: "Sites",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sites_AspNetUsers_LastModifiedById",
                table: "Sites",
                column: "LastModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sites_AspNetUsers_CreatedById",
                table: "Sites");

            migrationBuilder.DropForeignKey(
                name: "FK_Sites_AspNetUsers_LastModifiedById",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_CreatedById",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_LastModifiedById",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "LastModifiedById",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "Sites");
        }
    }
}
