using CcsSso.Shared.Contracts;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CcsSso.Shared.Services
{
  public class CryptographyService : ICryptographyService
  {
    public string EncryptString(string text, string keyValue)
    {
      try
      {
        var key = Encoding.UTF8.GetBytes(keyValue);

        using (var aesAlg = Aes.Create())
        {
          using (var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV))
          {
            using (var msEncrypt = new MemoryStream())
            {
              using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
              using (var swEncrypt = new StreamWriter(csEncrypt))
              {
                swEncrypt.Write(text);
              }

              var iv = aesAlg.IV;

              var decryptedContent = msEncrypt.ToArray();

              var result = new byte[iv.Length + decryptedContent.Length];

              Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
              Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

              var str = Convert.ToBase64String(result);
              var fullCipher = Convert.FromBase64String(str);
              return str;
            }
          }
        }
      }
      catch (Exception)
      {
        return string.Empty;
      }
    }

    public string DecryptString(string cipherText, string key)
    {
      if (string.IsNullOrEmpty(cipherText)) return cipherText;
      try
      {
        cipherText = cipherText.Replace(" ", "+");
        var fullCipher = Convert.FromBase64String(cipherText);

        var iv = new byte[16];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, fullCipher.Length - iv.Length);
        var keyEncoded = Encoding.UTF8.GetBytes(key);

        using (var aesAlg = Aes.Create())
        {
          using (var decryptor = aesAlg.CreateDecryptor(keyEncoded, iv))
          {
            string result;
            using (var msDecrypt = new MemoryStream(cipher))
            {
              using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
              {
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                  result = srDecrypt.ReadToEnd();
                }
              }
            }

            return result;
          }
        }
      }
      catch
      {
        return string.Empty;
      }
    }
  }
}
