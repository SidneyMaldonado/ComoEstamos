using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComoEstamos.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoverCategoriaParcela : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A categoria da parcela é sempre a da dívida (tb_divida.id_categoria). Parcelas com categoria
            // diferente da dívida perdem essa informação (decidido em 16/10/2026).
            migrationBuilder.DropForeignKey(
                name: "FK_tb_parcela_tb_categoria_id_categoria",
                table: "tb_parcela");

            migrationBuilder.DropIndex(
                name: "IX_tb_parcela_id_categoria",
                table: "tb_parcela");

            migrationBuilder.DropColumn(
                name: "id_categoria",
                table: "tb_parcela");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_categoria",
                table: "tb_parcela",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Volta com a categoria da dívida; dívida sem categoria (a coluna aceita nulo) fica com a primeira do usuário.
            migrationBuilder.Sql(
                "UPDATE tb_parcela SET id_categoria = (SELECT COALESCE(d.id_categoria, " +
                "(SELECT MIN(c.id_categoria) FROM tb_categoria c WHERE c.id_usuario = d.id_usuario)) " +
                "FROM tb_divida d WHERE d.id_divida = tb_parcela.id_divida);");

            migrationBuilder.CreateIndex(
                name: "IX_tb_parcela_id_categoria",
                table: "tb_parcela",
                column: "id_categoria");

            migrationBuilder.AddForeignKey(
                name: "FK_tb_parcela_tb_categoria_id_categoria",
                table: "tb_parcela",
                column: "id_categoria",
                principalTable: "tb_categoria",
                principalColumn: "id_categoria",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
