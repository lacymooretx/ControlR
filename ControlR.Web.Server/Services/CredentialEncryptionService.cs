using Microsoft.AspNetCore.DataProtection;

namespace ControlR.Web.Server.Services;

public interface ICredentialEncryptionService
{
  string Encrypt(string plaintext, Guid tenantId);
  string Decrypt(string ciphertext, Guid tenantId);
}

public class CredentialEncryptionService : ICredentialEncryptionService
{
  private readonly IDataProtectionProvider _dataProtection;

  public CredentialEncryptionService(IDataProtectionProvider dataProtection)
  {
    _dataProtection = dataProtection;
  }

  public string Encrypt(string plaintext, Guid tenantId)
  {
    var protector = _dataProtection.CreateProtector($"CredentialVault-{tenantId}");
    return protector.Protect(plaintext);
  }

  public string Decrypt(string ciphertext, Guid tenantId)
  {
    var protector = _dataProtection.CreateProtector($"CredentialVault-{tenantId}");
    return protector.Unprotect(ciphertext);
  }
}
