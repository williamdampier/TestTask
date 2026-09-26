using AngleSharp;
using AngleSharp.Dom;
using Dapper;
using Npgsql;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TestTask.Interfaces;
using TestTask.Models;

namespace TestTask.Services
{
    public class TestService : ITestService
    {
        private readonly string _connectionString;

        public TestService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private static readonly Regex EmailRegex =
            new Regex(
                @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}",
                RegexOptions.Compiled);

        public async Task<ResponseModel> Process(RequestModel request)
        {
            string url;
            try
            {
                var urlBytes = Convert.FromBase64String(request.UrlBase64!);
                url = System.Text.Encoding.UTF8.GetString(urlBytes);
            }
            catch (FormatException)
            {
                return new ResponseModel
                {
                    IsError = 1,
                    ErrorCode = "INVALID_URL_BASE64",
                    ErrorMessage = "URL is not valid Base64."
                };
            }

            string page;
            try
            {
                var pageBytes = Convert.FromBase64String(request.PageBase64!);
                page = System.Text.Encoding.UTF8.GetString(pageBytes);
            }
            catch (FormatException)
            {
                return new ResponseModel
                {
                    IsError = 1,
                    ErrorCode = "INVALID_PAGE_BASE64",
                    ErrorMessage = "Page is not valid Base64."
                };
            }

            byte[] encryptionBytes;
            byte[] keyBytes;
            try
            {
                encryptionBytes = Convert.FromBase64String(request.EncryptedTextBytesBase64!);
                keyBytes = Convert.FromBase64String(request.KeyBytesBase64!);
            }
            catch (FormatException)
            {
                return new ResponseModel
                {
                    IsError = 1,
                    ErrorCode = "INVALID_ENCRYPTION_BASE64",
                    ErrorMessage = "Encrypted text or key is not valid Base64."
                };
            }

            // требования AES-256: ключ должен быть ровно 32 байта
            if (keyBytes.Length != 32)
            {
                return new ResponseModel
                {
                    IsError = 1,
                    ErrorCode = "INVALID_AES_KEY",
                    ErrorMessage = "AES key must be 32 bytes."
                };
            }

            // AES работает блоками по 16 байт, поэтому длина encryptionBytes должна быть кратна 16.
            if (encryptionBytes.Length % 16 != 0)
            {
                return new ResponseModel
                {
                    IsError = 1,
                    ErrorCode = "INVALID_ENCRYPTED_DATA",
                    ErrorMessage = "Encrypted data length must be a multiple of 16 bytes."
                };
            }

            string decryptedPlainText;

            try
            {
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;

                using var decryptor = aes.CreateDecryptor();

                var decryptedBytes = decryptor.TransformFinalBlock(encryptionBytes, 0, encryptionBytes.Length);

                decryptedPlainText = System.Text.Encoding.UTF8.GetString(decryptedBytes);

            }
            catch (CryptographicException ex)
            {
                return new ResponseModel
                {
                    IsError = 1,
                    ErrorCode = "DECRYPTION_ERROR",
                    ErrorMessage = ex.Message
                };
            }

            try
            {
                var emailMatches = EmailRegex.Matches(page);
                var emailsList = emailMatches
                    .Select(match => match.Value)
                    .ToList();

                var emailsCount = emailsList.Count;

                var context = BrowsingContext.New(Configuration.Default);
                var document = await context.OpenAsync(req => req.Content(page));

                var elements = document.QuerySelectorAll(request.Selector!);
                var elementsCount = elements.Length;

                var elementsAttrList = new List<string>();

                await using var connection = new NpgsqlConnection(_connectionString);

                try
                {
                    await connection.OpenAsync();
                }
                catch (Exception ex)
                {
                    return new ResponseModel
                    {
                        IsError = 1,
                        ErrorCode = "DATABASE_CONNECTION_ERROR",
                        ErrorMessage = ex.Message
                    };
                }

                foreach (var element in elements)
                {
                    var attributeValue = element.GetAttribute(request.Attribute!);

                    if (attributeValue != null)
                    {
                        elementsAttrList.Add(attributeValue);
                    }

                    await connection.ExecuteAsync(
                        "INSERT INTO elements (attribute_value, html_code) values (@AttributeValue, @HtmlCode)",
                        new
                        {
                            AttributeValue = attributeValue,
                            HtmlCode = element.OuterHtml
                        }
                        );
                }

                return new ResponseModel
                {
                    Url = url,
                    ElementsCount = elementsCount,
                    ElementsAttrList = elementsAttrList,
                    EmailsCount = emailsCount,
                    EmailsList = emailsList,
                    DecryptedPlainText = decryptedPlainText
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel
                {
                    IsError = 1,
                    ErrorCode = "OTHER_ERROR",
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
