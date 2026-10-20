using ComoEstamos.Core.Formatacao;

namespace ComoEstamos.Test.ValidacaoTests
{
    [TestClass]
    public sealed class FormatoBrTests
    {
        [TestMethod]
        [DataRow("1.250,50", "1250.50")]
        [DataRow("1250,5", "1250.5")]
        [DataRow("-1.250,50", "-1250.50")]
        [DataRow("R$ 1.250,50", "1250.50")]
        [DataRow(" 10 ", "10")]
        [DataRow("1250.5", "1250.5")]
        [DataRow("1250.55", "1250.55")]
        [DataRow("0.000123", "0.000123")]
        [DataRow("1.5", "1.5")]
        [DataRow("0.123", "0.123")]
        [DataRow("-0.123", "-0.123")]
        [DataRow("1.000", "1000")]
        [DataRow("1.000.000", "1000000")]
        [DataRow("12.345,678901", "12345.678901")]
        public void TentarLerDecimal_LeFormatosAceitos(string texto, string esperado)
        {
            Assert.IsTrue(FormatoBr.TentarLerDecimal(texto, out var valor));
            Assert.AreEqual(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture), valor);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("abc")]
        [DataRow("1,2,3")]
        [DataRow("1.2.3,4")]
        [DataRow("--1")]
        [DataRow("1.25,00")]
        [DataRow("12.34.567")]
        public void TentarLerDecimal_RecusaTextoInvalido(string? texto)
        {
            Assert.IsFalse(FormatoBr.TentarLerDecimal(texto, out _));
        }

        [TestMethod]
        public void Moeda_FormataComDuasCasasEAteOMaximoInformado()
        {
            Assert.AreEqual("R$ 1.250,50", FormatoBr.Moeda(1250.5m));
            Assert.AreEqual("-R$ 1.250,50", FormatoBr.Moeda(-1250.5m));
            Assert.AreEqual("R$ 32,123456", FormatoBr.Moeda(32.123456m, casasMaximas: 6));
            Assert.AreEqual("R$ 32,50", FormatoBr.Moeda(32.5m, casasMaximas: 6));
        }

        [TestMethod]
        public void Numero_MostraSoAsCasasNecessarias()
        {
            Assert.AreEqual("1.000", FormatoBr.Numero(1000m, 6));
            Assert.AreEqual("0,000123", FormatoBr.Numero(0.000123m, 6));
            Assert.AreEqual("1.250,5", FormatoBr.Numero(1250.5m, 2));
        }

        [TestMethod]
        public void NumeroFixo_SempreMostraAsCasasPedidas()
        {
            Assert.AreEqual("1.250,00", FormatoBr.NumeroFixo(1250m, 2));
        }

        [TestMethod]
        [DataRow("1,005", DisplayName = "3 casas com vírgula")]
        [DataRow("1,000", DisplayName = "Zeros contam como casa digitada")]
        [DataRow("0.125", DisplayName = "Ponto decimal com 3 casas")]
        [DataRow("1.2555", DisplayName = "Ponto decimal com 4 casas")]
        [DataRow("1.250,005", DisplayName = "Milhar e 3 casas")]
        public void PassaDasCasas_RecusaMaisDeDuasCasas(string texto)
        {
            Assert.IsTrue(FormatoBr.PassaDasCasas(texto, 2));
        }

        [TestMethod]
        [DataRow("", DisplayName = "Vazio")]
        [DataRow("-", DisplayName = "Só o sinal")]
        [DataRow("1,", DisplayName = "Vírgula sem casas ainda")]
        [DataRow("1,5", DisplayName = "1 casa")]
        [DataRow("1.250,50", DisplayName = "Formato brasileiro")]
        [DataRow("1.250", DisplayName = "Ponto de milhar no meio da digitação")]
        [DataRow("1250.5", DisplayName = "Ponto decimal com 1 casa")]
        public void PassaDasCasas_AceitaAteDuasCasasETextoIncompleto(string texto)
        {
            Assert.IsFalse(FormatoBr.PassaDasCasas(texto, 2));
        }

        [TestMethod]
        public void DataHora_FormataOuMostraTracoQuandoNula()
        {
            Assert.AreEqual("07/10/2026 14:30", FormatoBr.DataHora(new DateTime(2026, 10, 7, 14, 30, 0)));
            Assert.AreEqual("—", FormatoBr.DataHora(null));
        }
    }
}
