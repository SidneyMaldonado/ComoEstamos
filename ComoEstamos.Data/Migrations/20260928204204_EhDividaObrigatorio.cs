using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComoEstamos.Data.Migrations
{
    /// <inheritdoc />
    public partial class EhDividaObrigatorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Registros sem tipo definido passam a ser dívida, que já era o padrão.
            migrationBuilder.Sql("UPDATE tb_divida SET dm_divida = 1 WHERE dm_divida IS NULL;");

            migrationBuilder.AlterColumn<bool>(
                name: "dm_divida",
                table: "tb_divida",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "dm_divida",
                table: "tb_divida",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");
        }
    }
}
