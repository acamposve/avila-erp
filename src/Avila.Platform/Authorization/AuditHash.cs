using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avila.Platform.Persistence;

namespace Avila.Platform.Authorization;

public static class AuditHash
{
    public static string Compute(AuditEvent audit)
    {
        var canonical = new
        {
            tenant_id = audit.TenantId.ToString("D"),
            actor_user_id = audit.ActorUserId?.ToString("D"),
            action = audit.Action,
            entity_type = audit.EntityType,
            entity_id = audit.EntityId?.ToString("D"),
            before = Canonicalize(audit.BeforeJson),
            after = Canonicalize(audit.AfterJson),
            correlation_id = audit.CorrelationId,
            occurred_at = audit.OccurredAt.ToUniversalTime().ToString("O"),
            previous_hash = audit.PreviousHash ?? string.Empty
        };

        var json = JsonSerializer.Serialize(canonical);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static string? Canonicalize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            Write(writer, document.RootElement);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject()
                             .OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    Write(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText(), skipInputValidation: false);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }
}
