using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComoEstamos.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoverUsuarioDivida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O dono da dívida passa a ser o da conta dela. Dívida sem conta (a coluna aceita nulo) recebe a
            // primeira conta do usuário, enquanto id_usuario existe; se o usuário não tem conta, a dívida fica
            // sem dono e some das telas (perda aceita em 16/10/2026).
            migrationBuilder.Sql(
                "UPDATE tb_divida SET id_conta = (SELECT MIN(c.id_conta) FROM tb_conta c " +
                "WHERE c.id_usuario = tb_divida.id_usuario) WHERE id_conta IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_divida_tb_usuario_id_usuario",
                table: "tb_divida");

            migrationBuilder.DropIndex(
                name: "IX_tb_divida_id_usuario",
                table: "tb_divida");

            migrationBuilder.DropColumn(
                name: "id_usuario",
                table: "tb_divida");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_usuario",
                table: "tb_divida",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Volta com o dono da conta da dívida; dívida sem conta (sem dono) fica com o primeiro usuário.
            migrationBuilder.Sql(
                "UPDATE tb_divida SET id_usuario = COALESCE((SELECT c.id_usuario FROM tb_conta c " +
                "WHERE c.id_conta = tb_divida.id_conta), (SELECT MIN(u.id_usuario) FROM tb_usuario u));");

            migrationBuilder.CreateIndex(
                name: "IX_tb_divida_id_usuario",
                table: "tb_divida",
                column: "id_usuario");

            migrationBuilder.AddForeignKey(
                name: "FK_tb_divida_tb_usuario_id_usuario",
                table: "tb_divida",
                column: "id_usuario",
                principalTable: "tb_usuario",
                principalColumn: "id_usuario",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
