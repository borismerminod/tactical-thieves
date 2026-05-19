using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TacticalThievesServer.Migrations
{
    /// <inheritdoc />
    public partial class POCModelReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "CurrentLevel",
                table: "PlayerProgress",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CurrentLevel",
                table: "PlayerProgress",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
