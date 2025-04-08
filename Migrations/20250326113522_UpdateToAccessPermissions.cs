using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloud9._2.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToAccessPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CanViewPage = table.Column<bool>(type: "bit", nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CanViewColumn = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessPermissions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessPermissions");
        }
    }
}
