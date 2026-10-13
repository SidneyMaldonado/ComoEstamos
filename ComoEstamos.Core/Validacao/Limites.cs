namespace ComoEstamos.Core.Validacao
{
    /// <summary>Maior valor absoluto que cabe em cada <c>Precision</c> usado nos models.</summary>
    public static class Limites
    {
        /// <summary><c>decimal(10,2)</c>: dinheiro (saldo, valor).</summary>
        public const decimal Decimal10x2 = 99_999_999.99m;

        /// <summary><c>decimal(18,6)</c>: quantidade e cotação de investimentos.</summary>
        public const decimal Decimal18x6 = 999_999_999_999.999999m;
    }
}
