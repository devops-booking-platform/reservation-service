using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservationService.Migrations
{
    /// <inheritdoc />
    public partial class idempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_GuestId_IdempotencyKey",
                table: "Reservations",
                columns: new[] { "GuestId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IdempotencyKey",
                table: "Reservations",
                column: "IdempotencyKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_GuestId_IdempotencyKey",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_IdempotencyKey",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Reservations");
        }
    }
}
