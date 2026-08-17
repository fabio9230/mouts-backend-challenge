using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations;

public partial class AddSaleIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SaleIdempotencyRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SaleIdempotencyRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_SaleIdempotencyRecords_Sales_SaleId",
                    column: x => x.SaleId,
                    principalTable: "Sales",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SaleIdempotencyRecords_Key",
            table: "SaleIdempotencyRecords",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SaleIdempotencyRecords_SaleId",
            table: "SaleIdempotencyRecords",
            column: "SaleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SaleIdempotencyRecords");
    }
}
