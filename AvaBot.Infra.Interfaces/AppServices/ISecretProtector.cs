namespace AvaBot.Infra.Interfaces.AppServices;

public interface ISecretProtector
{
    string Protect(string plain);
    string Unprotect(string cipher);
}
