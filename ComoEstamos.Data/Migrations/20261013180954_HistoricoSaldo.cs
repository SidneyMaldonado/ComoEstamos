using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComoEstamos.Data.Migrations
{
    /// <inheritdoc />
    public partial class HistoricoSaldo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_historico_saldo",
                columns: table => new
                {
                    id_historico_saldo = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    dt_historico_saldo = table.Column<DateTime>(type: "TEXT", nullable: false),
                    id_conta = table.Column<int>(type: "INTEGER", nullable: false),
                    nr_saldo = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_historico_saldo", x => x.id_historico_saldo);
                    table.ForeignKey(
                        name: "FK_tb_historico_saldo_tb_conta_id_conta",
                        column: x => x.id_conta,
                        principalTable: "tb_conta",
                        principalColumn: "id_conta",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_historico_saldo_id_conta",
                table: "tb_historico_saldo",
                column: "id_conta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_historico_saldo");
        }
    }
}
