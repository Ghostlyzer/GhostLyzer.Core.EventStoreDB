using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;

namespace GhostLyzer.Core.EventStoreDB.Serialization
{
    /// <summary>
    /// Provides extension methods for JSON serialization.
    /// </summary>
    public static class SerializationExtensions
    {
        /// <summary>
        /// Configures the provided <see cref="JsonSerializerSettings"/> with default settings.
        /// </summary>
        /// <param name="settings">The <see cref="JsonSerializerSettings"/> to configure.</param>
        /// <returns>The configured <see cref="JsonSerializerSettings"/>.</returns>
        public static JsonSerializerSettings WithDefaults(this JsonSerializerSettings settings)
        {
            settings.WithNonDefaultConstructorContractResolver()
                .Converters.Add(new StringEnumConverter());

            return settings;
        }

        /// <summary>
        /// Sets the ContractResolver of the provided <see cref="JsonSerializerSettings"/> to a new NonDefaultConstructorContractResolver.
        /// </summary>
        /// <param name="settings">The <see cref="JsonSerializerSettings"/> to configure.</param>
        /// <returns>The configured <see cref="JsonSerializerSettings"/>.</returns>
        public static JsonSerializerSettings WithNonDefaultConstructorContractResolver(this JsonSerializerSettings settings)
        {
            settings.ContractResolver = new NonDefaultConstructorContractResolver();
            return settings;
        }

        /// <summary>
        /// Deserializes a JSON string to a specific type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the JSON string to.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public static T FromJson<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(
                json, new JsonSerializerSettings().WithNonDefaultConstructorContractResolver())!;
        }

        /// <summary>
        /// Deserializes a JSON string to a specific type.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="type">The type to deserialize the JSON string to.</param>
        /// <returns>The deserialized object.</returns>
        public static object FromJson(this string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type,
                new JsonSerializerSettings().WithNonDefaultConstructorContractResolver())!;
        }

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>The JSON string representation of the object.</returns>
        public static string ToJson(this object obj) 
        {
            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// Serializes an object to a JSON string and wraps it in a <see cref="StringContent"/> instance.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A <see cref="StringContent"/> instance containing the JSON string representation of the object.</returns>
        public static StringContent ToJsonStirngContent(this object obj)
        {
            return new StringContent(obj.ToJson(), Encoding.UTF8, "application/json");
        }
    }
}
