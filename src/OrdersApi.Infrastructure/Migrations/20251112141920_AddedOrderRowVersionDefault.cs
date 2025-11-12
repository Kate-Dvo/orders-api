using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdersApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedOrderRowVersionDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "randomblob(8)",
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true,
                oldDefaultValueSql: "randomblob(8)");
        }
    }
}
