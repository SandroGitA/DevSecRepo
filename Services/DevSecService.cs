using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Dapper;
using FluentValidation;
using TestJob.Models;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace TestJob.Services;

public class DevSecService : IDevSecService
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private const string SqlQuery = """
            INSERT INTO elements (attribute_value, element_html)
            VALUES (@AttributeValue, @ElementHtml);
            """;

    private readonly IValidator<RequestDto> _validator;
    private readonly string _connectionString;

    public DevSecService(IValidator<RequestDto> validator, IConfiguration configuration)
    {
        _validator = validator;
        _connectionString = configuration.GetConnectionString("Postgres") ??
            throw new InvalidOperationException("Connection string 'Postgres' not found.");
    }

    public async Task<DevSecResult> ProcessAsync(RequestDto requestDto, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(requestDto, cancellationToken);
        if (!validation.IsValid)
        {
            return Failure(
                "VALIDATION_ERROR",
                string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
        }

        if (!TryDecodeBase64(requestDto.UrlB64, out var url))
        {
            return Failure(
                "INVALID_URL_BASE64",
                "Поле 'url_b64' должно содержать URL, закодированный в Base64.");
        }

        if (!TryDecodeBase64(requestDto.PageB64, out var htmlPage))
        {
            return Failure(
                "INVALID_PAGE_BASE64",
                "Поле 'page_b64' должно содержать HTML, закодированный в Base64.",
                Encoding.UTF8.GetString(url));
        }

        if (!TryDecodeBase64(requestDto.KeyBytesB64, out var key))
        {
            return Failure(
                "INVALID_KEY_BASE64",
                "Поле 'key_bytes_b64' должно содержать ключ, закодированный в Base64.",
                Encoding.UTF8.GetString(url));
        }

        if (key.Length != 32)
        {
            return Failure(
                "INVALID_KEY_LENGTH",
                "Ключ AES-256 должен содержать 32 байта.",
                Encoding.UTF8.GetString(url));
        }

        if (!TryDecodeBase64(requestDto.EncryptedTextBytesB64, out var encryptedText))
        {
            return Failure(
                "INVALID_CIPHERTEXT_BASE64",
                "Поле 'encrypted_text_bytes_b64' должно содержать шифротекст, закодированный в Base64.",
                Encoding.UTF8.GetString(url));
        }

        if (encryptedText.Length % 16 != 0)
        {
            return Failure(
                "INVALID_CIPHERTEXT_LENGTH",
                "Длина шифротекста должна быть кратна 16 байтам.",
                Encoding.UTF8.GetString(url));
        }

        var html = Encoding.UTF8.GetString(htmlPage);

        using var context = BrowsingContext.New(Configuration.Default);

        var document = await context.OpenAsync(req => req.Content(html), cancellationToken);
        IHtmlCollection<IElement> elements;

        try
        {
            elements = document.QuerySelectorAll(requestDto.Selector);
        }
        catch (DomException ex) when (ex.Code == (int)DomError.Syntax)
        {
            return Failure(
                "INVALID_SELECTOR",
                "Поле 'selector' должно содержать корректный CSS-селектор.",
                Encoding.UTF8.GetString(url));
        }

        var attributes = new List<string>();
        var dbElements = new List<ElementDbModel>();

        foreach (var element in elements)
        {
            var value = element.GetAttribute(requestDto.Attribute);
            if (value is not null)
                attributes.Add(value);

            dbElements.Add(new ElementDbModel(value, element.OuterHtml));
        }

        var emails = EmailRegex.Matches(html)
            .Select(match => match.Value)
            .ToList();

        using var aes = Aes.Create();
        aes.Key = key;
        var decryptedBytes = aes.DecryptEcb(encryptedText, PaddingMode.None);

        await using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            SqlQuery, dbElements, transaction: transaction, cancellationToken: cancellationToken));
            
        await transaction.CommitAsync(cancellationToken);

        return new DevSecResult
        {
            Url = Encoding.UTF8.GetString(url),
            DecryptedPlainText = Encoding.UTF8.GetString(decryptedBytes),
            ElementsCount = elements.Length,
            ElementsAttrList = attributes,
            EmailsCount = emails.Count,
            EmailsList = emails
        };
    }

    private static bool TryDecodeBase64(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static DevSecResult Failure(string code, string message, string url = "")
    {
        return new DevSecResult
        {
            IsError = 1,
            ErrorCode = code,
            ErrorMessage = message,
            Url = url,
            DecryptedPlainText = string.Empty
        };
    }
}
