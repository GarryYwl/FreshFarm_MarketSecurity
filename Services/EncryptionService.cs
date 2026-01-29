using Microsoft.AspNetCore.DataProtection;

namespace FreshFarmMarketSecurity.Services
{
    public class EncryptionService
    {
        private readonly IDataProtector _protector;

        public EncryptionService(IDataProtectionProvider provider)
        {
            // Purpose string should be unique to YOUR app
            _protector = provider.CreateProtector("FreshFarmMarket.CreditCard.v1");
        }

        public string Encrypt(string plaintext) => _protector.Protect(plaintext);

        public string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext);
    }
}
