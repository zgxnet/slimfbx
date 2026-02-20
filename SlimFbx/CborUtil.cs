
using PeterO.Cbor;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace SlimFbx;

public static partial class CborUtil
{
    public static void ConvertCborFileToJson(string fcbor, string? fjson = null)
    {
        CBORObject cbor = CBORObject.DecodeFromBytes(File.ReadAllBytes(fcbor));
        fjson ??= Path.ChangeExtension(fcbor, ".json");
        WriteJsonFile(fjson, cbor);
    }

    public static void WriteJsonFile(string fname, CBORObject cval)
    {
        JsonNode? data = ToJson(cval);
        string txt = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        File.WriteAllText(fname, txt);
    }

    public static JsonNode? ToJson(CBORObject val)
        => val.Type switch
        {
            CBORType.Boolean => JsonValue.Create(val.AsBoolean()),
            CBORType.SimpleValue => JsonValue.Create(val.SimpleValue),
            CBORType.ByteString =>JsonValue.Create(ToString(val.GetByteString())),
            CBORType.TextString => JsonValue.Create(val.AsString()),
            CBORType.Array => ArrayToJson(val),
            CBORType.Map => MapToJson(val),
            CBORType.Integer => JsonValue.Create(val.AsNumber().ToInt64Checked()),
            CBORType.FloatingPoint => JsonValue.Create(val.AsNumber().ToEFloat().ToDouble()),
            _ => null
        };

    static JsonNode ToJsonNumber(CBORNumber o)
        => o.Kind switch
        {
            CBORNumber.NumberKind.Integer => JsonValue.Create(o.ToInt64Checked()),
            _ => JsonValue.Create(o.ToEFloat().ToDouble())
        };

    static JsonArray ArrayToJson(CBORObject cobj)
    {
        JsonArray ja = new JsonArray();
        foreach (var o in cobj.Values)
        {
            JsonNode? jv = ToJson(o);
            ja.Add(jv);
        }
        return ja;
    }

    static JsonObject MapToJson(CBORObject cobj)
    {
        JsonObject jo = new JsonObject();
        foreach (var k in cobj.Keys)
        {
            JsonNode? jk = ToJson(k);
            JsonNode? jv = ToJson(cobj[k]);
            string key = jk?.ToString() ?? throw new Exception("Invalid cbor key");
            jo[key] = jv;
        }
        return jo;
    }

    static string ToString(ReadOnlyMemory<byte> data)
    {
        StringBuilder sb = new ($"<{data.Length}>");
        int len = Math.Min(data.Length, 16);
        ReadOnlySpan<byte> data1 = data.Span;
        static char ToHexChar(byte b)
        {
            if (b < 10) return (char)('0' + b);
            return (char)('A' + (b - 10));
        }
        for (int i = 0; i < len; i++)
        {
            byte b = data1[i];
            sb.Append(ToHexChar((byte)(b >> 4)));
            sb.Append(ToHexChar((byte)(b & 0x0F)));
        }
        if(len < data.Length)
        {
            sb.Append("...");
        }
        return sb.ToString();
    }
}
