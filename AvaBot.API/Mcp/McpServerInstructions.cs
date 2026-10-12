namespace AvaBot.API.Mcp;

/// <summary>Texto enviado ao assistente no initialize (contrato mcp-server.md).</summary>
public static class McpServerInstructions
{
    public const string Text =
        "Voce esta operando a conta de um usuario do AvaBot. Use quem_sou_eu para saber de quem. " +
        "Regras: (1) nao existe operacao para excluir agente; a exclusao e feita pelo usuario no painel. " +
        "(2) Voce nunca le nem grava credenciais: chave de API do modelo, Directory ID, App ID e segredo do Power BI, " +
        "tokens do Telegram e do WhatsApp. As respostas trazem apenas 'configurado' ou 'nao configurado'; se precisar de " +
        "uma credencial, peca ao usuario para informa-la no painel, em Agentes > (agente) > a aba correspondente. " +
        "(3) Todos os ids e listas sao da conta do dono da chave; um agente de outra conta responde 'nao encontrado'. " +
        "(4) Listas sao paginadas; peca as proximas paginas. " +
        "(5) testar_agente nao cria conversa publica, mas as consultas Power BI do teste entram no log de consultas.";
}
