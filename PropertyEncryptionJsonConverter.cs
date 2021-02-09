using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Reflection;

namespace System.Text.Json.Serialization.Encryption
{
    public class PropertyEncryptionJsonConverter<T> : JsonConverter<T>
    {

        private readonly IConfiguration _configuration;

        public PropertyEncryptionJsonConverter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(T).IsAssignableFrom(typeToConvert);
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var obj = JsonSerializer.Deserialize(ref reader, typeToConvert);

            var propertiesWithEncryptedAttribute = typeToConvert.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                  .Where(p => p.GetCustomAttributes(typeof(JsonEncryptAttribute), false).Count() == 1);

            foreach (var propertyWithEncryptedAttribute in propertiesWithEncryptedAttribute)
            {
                var encryptionKeyLookup = propertyWithEncryptedAttribute.GetCustomAttribute<JsonEncryptAttribute>().EncryptionKeyConfigurationPath;
                var encryptionKey = _configuration.GetSection(encryptionKeyLookup).Value;
                var valueToDecrypt = (string)propertyWithEncryptedAttribute.GetValue(obj);

                if (!string.IsNullOrWhiteSpace(valueToDecrypt))
                {
                    var decryptedValue = AesStringEncryption.Decrypt(valueToDecrypt, encryptionKey);
                    propertyWithEncryptedAttribute.SetValue(obj, decryptedValue);
                }
            }

            return (T)obj;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var propertiesWithEncryptedAttribute = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                  .Where(p => p.GetCustomAttributes(typeof(JsonEncryptAttribute), false).Count() == 1);

            foreach (var propertyWithEncryptedAttribute in propertiesWithEncryptedAttribute)
            {
                var encryptionKeyLookup = propertyWithEncryptedAttribute.GetCustomAttribute<JsonEncryptAttribute>().EncryptionKeyConfigurationPath;
                var encryptionKey = _configuration.GetSection(encryptionKeyLookup).Value;
                var valueToEncrypt = (string)propertyWithEncryptedAttribute.GetValue(value);

                if (!string.IsNullOrWhiteSpace(valueToEncrypt)) {
                    var encryptedValue = AesStringEncryption.Encrypt(valueToEncrypt, encryptionKey);
                    propertyWithEncryptedAttribute.SetValue(value, encryptedValue);
                }
            }

            JsonSerializer.Serialize(writer, value, value.GetType());
        }
    
    }
}
