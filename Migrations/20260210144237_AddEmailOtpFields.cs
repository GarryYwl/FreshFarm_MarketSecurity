using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreshFarmMarketSecurity.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOtpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TwoFactorOtpAttempts",
                table: "UserAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TwoFactorOtpExpiresAt",
                table: "UserAccounts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorOtpHash",
                table: "UserAccounts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFactorOtpAttempts",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "TwoFactorOtpExpiresAt",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "TwoFactorOtpHash",
                table: "UserAccounts");
        }
    }
}
