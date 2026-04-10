using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TacticalThievesServer.Migrations
{
    /// <inheritdoc />
    public partial class LinkPlayerProgressToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerProgress_Pseudo",
                table: "PlayerProgress");

            migrationBuilder.DropColumn(
                name: "Pseudo",
                table: "PlayerProgress");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PlayerProgress",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProgress_UserId",
                table: "PlayerProgress",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerProgress_Users_UserId",
                table: "PlayerProgress",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerProgress_Users_UserId",
                table: "PlayerProgress");

            migrationBuilder.DropIndex(
                name: "IX_PlayerProgress_UserId",
                table: "PlayerProgress");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PlayerProgress");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "Pseudo",
                table: "PlayerProgress",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProgress_Pseudo",
                table: "PlayerProgress",
                column: "Pseudo");
        }
    }
}
