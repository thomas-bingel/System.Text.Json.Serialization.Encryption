# Encrypting single properties with an attribute in System.Text.Json

In distributed systems it is common to connect microservices with a service bus. 
Depending on the configuration, messages are distributed to all of the services and processed if the service is interested in this message.
Therefore, all services see the message and data. What if I want to send sensitive date to a specific service, like a password?
In Azure it would be possible to configure this scenario. But I wanted to take a different approach.
I wanted to encrypt single properties of a message by just adding a attribute. Only then I 
could share a secret between two services and all others will not be able to read the value.

It should look something like this:

```C# 
public class UserCreatedEvent
{
    public Guid Id { get; set; }

    public string Username { get; set; }

    [JsonEncrypt("Encryption:UserPasswordKey")]  // Path to configuration
    public string Password { get; set; }
}
```
Other requirements I had in mind:
- The encryption secret should not be the same for all properties
- The encryption secret should not be hardcoded, but configurable using the IConfigurable interface. 

## The challenge

All this would be very easy when using *Newtonsoft.Json* because it has a lot of hooks you can use to achieve this goal.
The easiest would be writing a JsonConverter and add the configuration path as an parameter. Thats it.
But I wanted to use the *System.Text.Json* classes within the .NET Core framework. 

The *System.Text.Json.Serialization.JsonConverter* has some limitations, that makes a *nice* solution impossible.
First of all the *JsonConverterAttribute* does not allow to pass additional parameters. 
Second, it is not possible to access the properties of the currently processed de/serialized class. 
That means you cannot get the custom attribute with the path to the configured encryption key.

## The solution

After playing around with the JsonConvert and looking into the *System.Text.Json.Serialization.JsonConverter* class of the .NET Core runtime I got to this solution:

```CS 
// Simple class to be de/serialized. Property is annotated with a custom attribute

public class UserCreatedEvent
{
    public string Username { get; set; }

    [JsonEncrypt("Encryption:UserPasswordKey")]  // Path to configuration
    public string Password { get; set; }
}
```

The generic class of the *PropertyEncryptionJsonConverter* must pass the name of the class.

```CS
// How to use the PropertyEncyptionJsonConverter 

var jsonOptions = new JsonSerializerOptions();
jsonOptions.Converters.Add(new PropertyEncryptionJsonConverter<UserCreatedEvent>(Configuration));

// Serialize (add the jsonOptions)
var jsonString = JsonSerializer.Serialize(new UserCreatedEvent()
 { 
    Password = "My password" 
 }, jsonOptions );

// Deserialize (add the jsonOptions)
var obj = JsonSerializer.Deserialize<UserCreatedEvent>(jsonString, jsonOptions);
```

The encryption key can be configured in any IConfigurationProvider. 
So it can come from the Azure Key Store, environment variable or simply the appsettings.json 

```JSON
"Encryption": {
  "UserPasswordKey": "myEncryptionKeySuperSecure"
}
```


## Behind the scenes

The JsonConverter looks through all the properties marked with the *JsonEncryptAttribute*.
When found the value is read via reflections, encrypted, and wrote back to the property. 
After that, the regular serialization takes place. For deserialization it is just the way around.

```C#
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
```

## Limitations

Limitations owed by *System.Text.Json.Serialization.JsonConverter* are:
- You must add the converter to the serializer for each class or one super-class. 

Other limitations can be overcome by a little coding:
- Only strings are encrypted. 
- Properties in subclasses marked to be encrypted will not be processed.

