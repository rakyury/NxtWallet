using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace NxtWallet.Core.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contact",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    NxtAddressRs = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactDto", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "LedgerEntry",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountFrom = table.Column<string>(nullable: false),
                    AccountTo = table.Column<string>(nullable: true),
                    BlockId = table.Column<long>(nullable: true),
                    Height = table.Column<int>(nullable: true),
                    IsConfirmed = table.Column<bool>(nullable: false, defaultValue: true),
                    Message = table.Column<string>(nullable: true),
                    NqtAmount = table.Column<long>(nullable: false),
                    NqtBalance = table.Column<long>(nullable: false),
                    NqtFee = table.Column<long>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    TransactionId = table.Column<long>(nullable: true),
                    TransactionType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntryDto", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "Setting",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingDto", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("Contact");
            migrationBuilder.DropTable("LedgerEntry");
            migrationBuilder.DropTable("Setting");
        }
    }
}