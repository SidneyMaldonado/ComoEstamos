using System.Globalization;
using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Validacao;

namespace ComoEstamos.Pages
{
    /// <summary>
    /// Passos que se repetem nas páginas de CRUD (Spec/07-Crud.md): ler o Id da rota, ler números
    /// digitados, mostrar as mensagens de erro abaixo dos campos, salvar, excluir e voltar.
    /// </summary>
    internal static class Formulario
    {
        /// <summary>Id recebido na rota (<c>?id=3</c>), ou 0 se não veio.</summary>
        public static int LerId(IDictionary<string, object> query) =>
            query.TryGetValue("id", out var valor) && int.TryParse(valor?.ToString(), out var id) ? id : 0;

        public static void MostrarErro(Label label, string? mensagem)
        {
            label.Text = mensagem;
            label.IsVisible = mensagem is not null;
        }

        public static void LimparErros(params Label[] labels)
        {
            foreach (var label in labels)
                MostrarErro(label, null);
        }

        /// <summary>
        /// Lê o número digitado no campo. Se estiver vazio ou fora do formato, mostra a mensagem
        /// abaixo dele e devolve <c>false</c>. A faixa e as casas decimais são validadas no caso de uso.
        /// </summary>
        /// <param name="rotulo">Nome do campo com artigo, em minúsculas ("o saldo").</param>
        /// <param name="exemplo">Exemplo do formato esperado ("1.250,00").</param>
        public static bool TentarLerDecimal(Entry campo, Label erro, string rotulo, string exemplo, out decimal valor)
        {
            if (string.IsNullOrWhiteSpace(campo.Text))
            {
                MostrarErro(erro, $"Informe {rotulo}.");
                valor = 0;
                return false;
            }
            if (!FormatoBr.TentarLerDecimal(campo.Text, out valor))
            {
                MostrarErro(erro, $"Valor inválido. Use o formato {exemplo}.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Lê o número inteiro digitado no campo. Se estiver vazio ou não for um inteiro, mostra a
        /// mensagem abaixo dele e devolve <c>false</c>. A faixa é validada no caso de uso.
        /// </summary>
        /// <param name="rotulo">Nome do campo com artigo, em minúsculas ("o número de parcelas").</param>
        public static bool TentarLerInteiro(Entry campo, Label erro, string rotulo, out int valor)
        {
            if (string.IsNullOrWhiteSpace(campo.Text))
            {
                MostrarErro(erro, $"Informe {rotulo}.");
                valor = 0;
                return false;
            }
            if (!int.TryParse(campo.Text.Trim(), NumberStyles.None, FormatoBr.Cultura, out valor))
            {
                MostrarErro(erro, "Valor inválido. Use só números, sem vírgula.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Executa a gravação com o botão desabilitado. Erros de validação aparecem abaixo de cada
        /// campo (<paramref name="erros"/>: propriedade → Label); os demais, num alerta.
        /// Devolve <c>true</c> se gravou.
        /// </summary>
        public static async Task<bool> SalvarAsync(Page pagina, Button botao, Func<Task> salvar, IReadOnlyDictionary<string, Label> erros)
        {
            botao.IsEnabled = false;
            try
            {
                await salvar();
                return true;
            }
            catch (ValidacaoException ex)
            {
                var semCampo = new List<string>();
                foreach (var erro in ex.Erros)
                {
                    if (erros.TryGetValue(erro.Campo, out var label))
                        MostrarErro(label, erro.Mensagem);
                    else
                        semCampo.Add(erro.Mensagem);
                }
                if (semCampo.Count > 0)
                    await pagina.DisplayAlertAsync("Verifique os dados", string.Join(Environment.NewLine, semCampo), "OK");
                return false;
            }
            catch (Exception ex)
            {
                await pagina.DisplayAlertAsync("Não foi possível salvar", ex.Message, "OK");
                return false;
            }
            finally
            {
                botao.IsEnabled = true;
            }
        }

        /// <summary>
        /// Pede a confirmação da exclusão. "Sim" exclui e volta para a lista; "Não" fica na página.
        /// Se a exclusão for recusada (ex.: há dependentes), mostra a mensagem e fica na página.
        /// </summary>
        public static async Task ConfirmarEExcluirAsync(Page pagina, string titulo, string pergunta, Func<Task> excluir)
        {
            if (!await pagina.DisplayAlertAsync(titulo, pergunta, "Sim", "Não"))
                return;
            try
            {
                await excluir();
                await VoltarAsync();
            }
            catch (Exception ex)
            {
                await pagina.DisplayAlertAsync("Não foi possível excluir", ex.Message, "OK");
            }
        }

        /// <summary>Avisa que o registro não existe mais (ou foi excluído) e volta.</summary>
        public static async Task AvisarNaoEncontradoAsync(Page pagina, string titulo, string mensagem)
        {
            await pagina.DisplayAlertAsync(titulo, mensagem, "OK");
            await VoltarAsync();
        }

        public static Task VoltarAsync() => Shell.Current.GoToAsync("..");
    }
}
