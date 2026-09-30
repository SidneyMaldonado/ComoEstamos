using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComoEstamos.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_usuario",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nm_usuario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ds_email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ds_senha = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    img_usuario = table.Column<byte[]>(type: "BLOB", nullable: true),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_usuario", x => x.id_usuario);
                });

            migrationBuilder.CreateTable(
                name: "tb_carteira",
                columns: table => new
                {
                    id_carteira = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_usuario = table.Column<int>(type: "INTEGER", nullable: false),
                    nm_carteira = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_carteira", x => x.id_carteira);
                    table.ForeignKey(
                        name: "FK_tb_carteira_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_categoria",
                columns: table => new
                {
                    id_categoria = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_usuario = table.Column<int>(type: "INTEGER", nullable: false),
                    nm_categoria = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    img_categoria = table.Column<byte[]>(type: "BLOB", nullable: true),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_categoria", x => x.id_categoria);
                    table.ForeignKey(
                        name: "FK_tb_categoria_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_conta",
                columns: table => new
                {
                    id_conta = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_usuario = table.Column<int>(type: "INTEGER", nullable: false),
                    nm_conta = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    img_conta = table.Column<byte[]>(type: "BLOB", nullable: true),
                    nr_saldo = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_conta", x => x.id_conta);
                    table.ForeignKey(
                        name: "FK_tb_conta_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_credor",
                columns: table => new
                {
                    id_credor = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_usuario = table.Column<int>(type: "INTEGER", nullable: false),
                    nm_credor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ds_observacoes = table.Column<string>(type: "TEXT", nullable: true),
                    img_logo = table.Column<byte[]>(type: "BLOB", nullable: true),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_credor", x => x.id_credor);
                    table.ForeignKey(
                        name: "FK_tb_credor_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_investimento",
                columns: table => new
                {
                    id_investimento = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_carteira = table.Column<int>(type: "INTEGER", nullable: false),
                    nm_investimento = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    nr_quantidade = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    vl_cotacao = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ds_observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_investimento", x => x.id_investimento);
                    table.ForeignKey(
                        name: "FK_tb_investimento_tb_carteira_id_carteira",
                        column: x => x.id_carteira,
                        principalTable: "tb_carteira",
                        principalColumn: "id_carteira",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_divida",
                columns: table => new
                {
                    id_divida = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_usuario = table.Column<int>(type: "INTEGER", nullable: false),
                    id_credor = table.Column<int>(type: "INTEGER", nullable: true),
                    id_conta = table.Column<int>(type: "INTEGER", nullable: true),
                    id_categoria = table.Column<int>(type: "INTEGER", nullable: true),
                    nm_divida = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    dia_vencimento = table.Column<int>(type: "INTEGER", nullable: false),
                    dt_primeiro_vencimento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    nr_parcelas = table.Column<int>(type: "INTEGER", nullable: false),
                    nr_valor = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    dm_divida = table.Column<bool>(type: "INTEGER", nullable: true),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_divida", x => x.id_divida);
                    table.CheckConstraint("CK_tb_divida_dia_vencimento", "dia_vencimento >= 1 AND dia_vencimento <= 31");
                    table.CheckConstraint("CK_tb_divida_nr_valor", "CAST(nr_valor AS REAL) >= 0");
                    table.ForeignKey(
                        name: "FK_tb_divida_tb_categoria_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "tb_categoria",
                        principalColumn: "id_categoria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_divida_tb_conta_id_conta",
                        column: x => x.id_conta,
                        principalTable: "tb_conta",
                        principalColumn: "id_conta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_divida_tb_credor_id_credor",
                        column: x => x.id_credor,
                        principalTable: "tb_credor",
                        principalColumn: "id_credor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_divida_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_historico",
                columns: table => new
                {
                    id_historico = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    dt_historico = table.Column<DateTime>(type: "TEXT", nullable: false),
                    id_investimento = table.Column<int>(type: "INTEGER", nullable: false),
                    nm_investimento = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    nr_quantidade = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    vl_cotacao = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ds_observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_historico", x => x.id_historico);
                    table.ForeignKey(
                        name: "FK_tb_historico_tb_investimento_id_investimento",
                        column: x => x.id_investimento,
                        principalTable: "tb_investimento",
                        principalColumn: "id_investimento",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_operacao",
                columns: table => new
                {
                    id_operacao = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_investimento = table.Column<int>(type: "INTEGER", nullable: false),
                    dm_compra = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_operacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    nr_quantidade = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    vl_operacao = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_operacao", x => x.id_operacao);
                    table.ForeignKey(
                        name: "FK_tb_operacao_tb_investimento_id_investimento",
                        column: x => x.id_investimento,
                        principalTable: "tb_investimento",
                        principalColumn: "id_investimento",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_parcela",
                columns: table => new
                {
                    id_parcela = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_divida = table.Column<int>(type: "INTEGER", nullable: false),
                    id_categoria = table.Column<int>(type: "INTEGER", nullable: false),
                    id_conta = table.Column<int>(type: "INTEGER", nullable: false),
                    ds_parcela = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    nr_valor = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    dt_vencimento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_pagamento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    dm_ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    dt_criacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    dt_alteracao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_parcela", x => x.id_parcela);
                    table.ForeignKey(
                        name: "FK_tb_parcela_tb_categoria_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "tb_categoria",
                        principalColumn: "id_categoria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_parcela_tb_conta_id_conta",
                        column: x => x.id_conta,
                        principalTable: "tb_conta",
                        principalColumn: "id_conta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_parcela_tb_divida_id_divida",
                        column: x => x.id_divida,
                        principalTable: "tb_divida",
                        principalColumn: "id_divida",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_carteira_id_usuario",
                table: "tb_carteira",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tb_categoria_id_usuario",
                table: "tb_categoria",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tb_conta_id_usuario",
                table: "tb_conta",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tb_credor_id_usuario",
                table: "tb_credor",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tb_divida_id_categoria",
                table: "tb_divida",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_tb_divida_id_conta",
                table: "tb_divida",
                column: "id_conta");

            migrationBuilder.CreateIndex(
                name: "IX_tb_divida_id_credor",
                table: "tb_divida",
                column: "id_credor");

            migrationBuilder.CreateIndex(
                name: "IX_tb_divida_id_usuario",
                table: "tb_divida",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tb_historico_id_investimento",
                table: "tb_historico",
                column: "id_investimento");

            migrationBuilder.CreateIndex(
                name: "IX_tb_investimento_id_carteira",
                table: "tb_investimento",
                column: "id_carteira");

            migrationBuilder.CreateIndex(
                name: "IX_tb_operacao_id_investimento",
                table: "tb_operacao",
                column: "id_investimento");

            migrationBuilder.CreateIndex(
                name: "IX_tb_parcela_id_categoria",
                table: "tb_parcela",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_tb_parcela_id_conta",
                table: "tb_parcela",
                column: "id_conta");

            migrationBuilder.CreateIndex(
                name: "IX_tb_parcela_id_divida",
                table: "tb_parcela",
                column: "id_divida");

            migrationBuilder.CreateIndex(
                name: "UQ_tb_usuario_ds_email",
                table: "tb_usuario",
                column: "ds_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_historico");

            migrationBuilder.DropTable(
                name: "tb_operacao");

            migrationBuilder.DropTable(
                name: "tb_parcela");

            migrationBuilder.DropTable(
                name: "tb_investimento");

            migrationBuilder.DropTable(
                name: "tb_divida");

            migrationBuilder.DropTable(
                name: "tb_carteira");

            migrationBuilder.DropTable(
                name: "tb_categoria");

            migrationBuilder.DropTable(
                name: "tb_conta");

            migrationBuilder.DropTable(
                name: "tb_credor");

            migrationBuilder.DropTable(
                name: "tb_usuario");
        }
    }
}
