using LanChat.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace LanChat.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CryptoController : ControllerBase
    {
        private readonly KeyVaultCryptoService _cryptoService;

        public CryptoController(KeyVaultCryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        [HttpPost("encrypt")]
        public async Task<IActionResult> EncryptMessage([FromBody] string plainText)
        {
            string cipherText = await _cryptoService.EncryptTextAsync(plainText);
            
            return Ok(new { Original = plainText, Encrypted = cipherText });
        }

        [HttpPost("decrypt")]
        public async Task<IActionResult> DecryptMessage([FromBody] string cipherText)
        {
            string plainText = await _cryptoService.DecryptTextAsync(cipherText);
            
            return Ok(new { Encrypted = cipherText, Decrypted = plainText });
        }
    }
}
