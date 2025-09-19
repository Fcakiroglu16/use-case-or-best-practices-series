using OpenTelemetry;
using OpenTelemetry.Logs;
using System.Collections;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Todo.API.OpenTelemetry.Processing
{
    public sealed class UnifiedRedactionProcessor : BaseProcessor<LogRecord>
    {
        private readonly JsonSerializerOptions _json;
        private readonly bool _alsoConvertBodyAndRedact;

        public UnifiedRedactionProcessor(
            JsonSerializerOptions? json = null,
            bool alsoConvertBodyAndRedact = false)
        {
            _json = json ?? new JsonSerializerOptions { WriteIndented = false };
            _alsoConvertBodyAndRedact = alsoConvertBodyAndRedact;
        }

        // ---- Key denylist (normalize: lowercase, remove '-' and '_') ----
        private static readonly HashSet<string> KeyDeny = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "passwd", "pwd", "secret", "clientsecret", "token",
            "accesstoken", "refreshtoken", "authorization", "setcookie", "cookie",
            "xapikey", "apikey", "sessionid", "creditcard", "cardnumber", "identityNumber"
        };

        // ---- Patterns to redact (tune to your needs) ----
        private static readonly Regex Jwt =
            new(@"\beyJ[A-Za-z0-9_\-]+?\.[A-Za-z0-9_\-]+?\.[A-Za-z0-9_\-]+?\b",
                RegexOptions.Compiled);

        private static readonly Regex IbanTR =
            new(@"\bTR\d{24}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CreditCard =
            new(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.Compiled);

        private static readonly Regex TcKimlik =
            new(@"\b\d{11}\b", RegexOptions.Compiled);

        private static readonly Regex Email =
            new(@"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b",
                RegexOptions.Compiled);

        [Obsolete]
        public override void OnEnd(LogRecord record)
        {
            // 1) Start with existing attributes (sanitize in place)
            var attrs = record.Attributes is { Count: > 0 }
                ? (List<KeyValuePair<string, object?>>)[.. record.Attributes!]
                : [];

            for (int i = 0; i < attrs.Count; i++)
            {
                var (key, val) = attrs[i];

                if (IsSensitiveKey(key))
                {
                    attrs[i] = new KeyValuePair<string, object?>(key, MaskHalfString(val.ToString()));
                    continue;
                }

                var masked = RedactValueObject(val);
                if (!ReferenceEquals(masked, val))
                    attrs[i] = new KeyValuePair<string, object?>(key, masked!);
            }

            // 2) Bring StateValues into attributes (sanitized)
            //    Note: StateValues is read-only; we *copy* safe versions into Attributes.
            if (record.StateValues is { Count: > 0 } state)
            {
                foreach (var kv in state)
                {
                    var key = kv.Key;
                    var val = kv.Value;

                    if (string.IsNullOrEmpty(key)) continue;

                    if (IsSensitiveKey(key))
                    {
                        attrs.Add(new KeyValuePair<string, object>(NormalizeKey(key), MaskHalfString(val.ToString())));
                    }
                    else
                    {
                        attrs.Add(new KeyValuePair<string, object>(NormalizeKey(key), RedactValueObject(val)!));
                    }
                }
            }

            record.Attributes = attrs;

            // 3) (Optional) redact body too
            if (_alsoConvertBodyAndRedact && record.Body is not null)
            {
                record.Body = (string)RedactValueObject(record.Body)!;
            }
        }

        private static string NormalizeKey(string key)
            => key.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        private static bool IsSensitiveKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            var norm = key.Replace("-", "", StringComparison.Ordinal)
                .Replace("_", "", StringComparison.Ordinal);
            return KeyDeny.Contains(norm);
        }

        private object? RedactValueObject(object? value)
        {
            if (value is null) return null;

            var t = value.GetType();

            // primitives/known simple types → keep (but still pattern-scan if string)
            if (IsSimple(t))
            {
                if (value is string s) return RedactString(s);
                return value;
            }

            // Collections/objects → JSON then pattern mask the JSON string
            var json = SafeSerialize(value);
            return json is null ? "******" : RedactString(json);
        }

        private static bool IsSimple(Type t)
        {
            if (t.IsPrimitive || t.IsEnum) return true;

            if (t == typeof(string) || t == typeof(decimal) ||
                t == typeof(DateTime) || t == typeof(DateTimeOffset) ||
                t == typeof(TimeSpan) || t == typeof(Guid))
                return true;

            var nt = Nullable.GetUnderlyingType(t);
            if (nt is not null) return IsSimple(nt);

            // treat IEnumerable/IDictionary as complex (force JSON)
            if (typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string))
            {
            }

            return false;
        }

        private string? SafeSerialize(object obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj, _json);
            }
            catch
            {
                return null;
            }
        }

        private static string RedactString(string input)
        {
            // Order matters: specific/long tokens first
            var s = input;
            s = Jwt.Replace(s, "******");
            s = IbanTR.Replace(s, m => MaskMid(m.Value));
            s = CreditCard.Replace(s, m => KeepLast4(m.Value));
            s = TcKimlik.Replace(s, m => MaskMid(m.Value));
            s = Email.Replace(s, m => MaskEmail(m.Value));
            return s;
        }

        private static string KeepLast4(string source)
        {
            var digits = new string(source.Where(char.IsDigit).ToArray());
            if (digits.Length <= 4) return "******";
            var keep = digits[^4..];
            return $"***-***-***-{keep}";
        }

        private static string MaskMid(string value)
        {
            if (value.Length <= 6) return "******";
            var head = value[..3];
            var tail = value[^3..];
            return $"{head}***{tail}";
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return "******";
            var local = email[..at];
            var domain = email[(at + 1)..];
            var maskedLocal = local.Length <= 2
                ? "*"
                : local[0] + new string('*', local.Length - 2) + local[^1];
            return $"{maskedLocal}@{domain}";
        }

        private static string MaskHalfString(string value)
        {
            int totalLength = value.Length;
            int halfLength = totalLength / 2;
            int keepStart = (totalLength - halfLength) / 2;
            int keepEnd = totalLength - keepStart - halfLength;

            string start = value.Substring(0, keepStart);
            string end = value.Substring(totalLength - keepEnd);
            string mask = new string('*', halfLength);

            return $"{start}{mask}{end}";
        }
    }
}