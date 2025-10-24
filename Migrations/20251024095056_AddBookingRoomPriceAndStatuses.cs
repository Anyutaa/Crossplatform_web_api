using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crossplatform_2_smirnova.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingRoomPriceAndStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "IsArchived",
                table: "Users",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Rooms",
                newName: "Status");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAtBooking",
                table: "BookingRooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceAtBooking",
                table: "BookingRooms");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Users",
                newName: "IsArchived");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Rooms",
                newName: "IsActive");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
