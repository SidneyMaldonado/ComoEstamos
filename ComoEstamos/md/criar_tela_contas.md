#Sempre será soft delete
# As listgens só devem mostrar dm_ativo = true.
# A saber: nm_ significa nome, ds_ significa descricao, nr_ significa numero, dt significa data dm significa Dominio (Booleano)
Crud de Contas

- Utilize a mesma imagem de fundo.
Serão 5 telas a saber
 - Listar Contas
 - Incluir Conta
 - Alterar Conta

A pagina Listar Contas:
   - deve ter uma lista de Contas, similar a da main page.
   - Quando a linha de uma conta for tocada deve redirecionar para o alterar conta
   - No cando superior direito deve ter um sinal de Mais em Dourado Com um circulo no fundo e quando
     for clicado deve redirecionar para a tela de Incluir Conta.
   - A pagina de Incluir conta:
       - Deve fazer um formuário com os dados necessários para incluir a conta. 
       - O títulos dos campos devem ser ajustados para o portugues correto. Exemplo: nm_conta deve ser Nome da Conta.
       - Deve ter os botões Salvar e Cancelar no fim do formuário.
         - Botão cancelar deve voltar para a Página de Lista
         - Botão Salvar deve incluir a conta no banco de dados e retornar para a lista de Contas.
       - Antes de gravar deve validar o dados se estão corretos e preenchidos.
   - A página de alterar e similar a tela de incluir:
     - Deve carregar os dados do registro de foi tocado na tela de Listar Contas.
     - no botao salvr deve atualizar os dados e não incluir
     - Também deve validar se todos os dados estão corretos.
     - Também deve ter o ícone da lixeira, no canto superior direito e quando for clicado abre um popup ou modal que:
         - deve perguntar se confirma a exclusão:
             - Se sim gava dm_ativo = false para aquele registros.
             - Se não volta para a ela de Alterar conta.

 