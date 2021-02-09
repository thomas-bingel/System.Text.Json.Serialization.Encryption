
namespace System.Text.Json.Serialization.Encryption
{
    /// <summary>
    /// By adding this to a property of a class to be serialized as json, this attribute will be encrypted.
    /// The encryption key will be loaded from the IConfiguration interface
    /// </summary>
    public class JsonEncryptAttribute : Attribute
    {
        public string EncryptionKeyConfigurationPath { get; set; }

        public JsonEncryptAttribute(string encryptionKeyConfigurationPath)
        {
            this.EncryptionKeyConfigurationPath = encryptionKeyConfigurationPath;
        }
    }
}
