using ComoEstamos.Core;
using ComoEstamos.Core.Validacao;

namespace ComoEstamos.Test.ValidacaoTests
{
    [TestClass]
    public sealed class ValidadorCamposTests
    {
        [TestMethod]
        public void Texto_ObrigatorioVazioPedeOCampo()
        {
            var validador = new ValidadorCampos();

            validador.Texto("Nome", "o nome da conta", "", 100, obrigatorio: true);

            Assert.AreEqual(new ErroValidacao("Nome", "Informe o nome da conta."), validador.Erros.Single());
        }

        [TestMethod]
        public void Texto_OpcionalVazioOuNuloEhValido()
        {
            var validador = new ValidadorCampos();

            validador.Texto("Observacao", "a observação", null, 500, obrigatorio: false);
            validador.Texto("Observacao", "a observação", "", 500, obrigatorio: false);

            Assert.IsEmpty(validador.Erros);
        }

        [TestMethod]
        public void Texto_RecusaAcimaDoTamanhoMaximoEAceitaNoLimite()
        {
            var validador = new ValidadorCampos();

            validador.Texto("A", "o nome", new string('x', 50), 50, obrigatorio: true);
            validador.Texto("B", "o nome", new string('x', 51), 50, obrigatorio: true);

            Assert.AreEqual(new ErroValidacao("B", "O nome deve ter no máximo 50 caracteres."), validador.Erros.Single());
        }

        [TestMethod]
        public void Texto_SemTamanhoMaximoAceitaQualquerTamanho()
        {
            var validador = new ValidadorCampos();

            validador.Texto("Observacoes", "as observações", new string('x', 10_000), tamanhoMaximo: null, obrigatorio: false);

            Assert.IsEmpty(validador.Erros);
        }

        [TestMethod]
        public void Numero_RecusaCasasDecimaisAMais()
        {
            var validador = new ValidadorCampos();

            validador.Numero("Saldo", "o saldo", 10.123m, 2, -Limites.Decimal10x2, Limites.Decimal10x2);

            Assert.AreEqual("O saldo deve ter no máximo 2 casas decimais.", validador.Erros.Single().Mensagem);
        }

        [TestMethod]
        public void Numero_RecusaForaDaFaixaEAceitaOsLimites()
        {
            var validador = new ValidadorCampos();

            validador.Numero("A", "o saldo", Limites.Decimal10x2, 2, -Limites.Decimal10x2, Limites.Decimal10x2);
            validador.Numero("B", "o saldo", -Limites.Decimal10x2, 2, -Limites.Decimal10x2, Limites.Decimal10x2);
            validador.Numero("C", "a quantidade", -0.000001m, 6, 0, Limites.Decimal18x6);

            var erro = validador.Erros.Single();
            Assert.AreEqual("C", erro.Campo);
            Assert.AreEqual("A quantidade deve estar entre 0 e 999.999.999.999,999999.", erro.Mensagem);
        }

        [TestMethod]
        public void Adicionar_GuardaSoOPrimeiroErroDeCadaCampo()
        {
            var validador = new ValidadorCampos();

            validador.Adicionar("Nome", "primeiro");
            validador.Adicionar("Nome", "segundo");
            validador.Adicionar("Saldo", "outro campo");

            CollectionAssert.AreEqual(new[] { "primeiro", "outro campo" }, validador.Erros.Select(e => e.Mensagem).ToArray());
            Assert.IsTrue(validador.TemErro("Nome"));
            Assert.IsFalse(validador.TemErro("Cotacao"));
        }

        [TestMethod]
        public void LancarSeHouverErros_LancaTodosOsErrosJuntos()
        {
            var validador = new ValidadorCampos();
            validador.LancarSeHouverErros();
            validador.Adicionar("Nome", "Informe o nome.");
            validador.Adicionar("Saldo", "Saldo inválido.");

            var ex = Assert.ThrowsExactly<ValidacaoException>(validador.LancarSeHouverErros);

            Assert.HasCount(2, ex.Erros);
            Assert.AreEqual("Informe o nome." + Environment.NewLine + "Saldo inválido.", ex.Message);
            Assert.IsInstanceOfType<RegraDeNegocioException>(ex);
        }

        [TestMethod]
        public void Normalizar_RemoveEspacosEOpcionalVazioViraNulo()
        {
            Assert.AreEqual("Nubank", ValidadorCampos.Normalizar("  Nubank "));
            Assert.AreEqual("", ValidadorCampos.Normalizar(null));
            Assert.IsNull(ValidadorCampos.NormalizarOpcional("   "));
            Assert.AreEqual("obs", ValidadorCampos.NormalizarOpcional(" obs "));
        }

        [TestMethod]
        public void MesmoNome_IgnoraMaiusculasEEspacos()
        {
            Assert.IsTrue(ValidadorCampos.MesmoNome("Ação ", "AÇÃO"));
            Assert.IsFalse(ValidadorCampos.MesmoNome("Ação", "Acao"));
        }
    }
}
