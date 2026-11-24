namespace AvaBot.Domain.Models;

/// <summary>Textos padrao das frases prontas do fluxo de solicitacao de acesso (feature 018).</summary>
public static class WhatsappAccessDefaults
{
    public const int MaxMessageLength = 500;

    public const string OfferMessage = "Me desculpe, você não possui acesso. Deseja solicitar agora? Responda apenas \"sim\" ou \"não\".";
    public const string ConfirmationMessage = "Seu acesso foi solicitado. Assim que for aprovado, você poderá fazer mais perguntas.";
    public const string WaitingMessage = "Ainda estamos aguardando a aprovação do seu acesso.";
    public const string DeclinedMessage = "Tudo bem. Se mudar de ideia, é só enviar uma nova mensagem.";
}
