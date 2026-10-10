namespace AvaBot.Infra.Interfaces.AppServices;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}
