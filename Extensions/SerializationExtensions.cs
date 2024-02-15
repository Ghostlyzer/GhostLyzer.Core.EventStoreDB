using GhostLyzer.Core.EventStoreDB.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;

namespace GhostLyzer.Core.EventStoreDB.Extensions
{
    /// <summary>
    /// Provides extension methods for JSON serialization.
    /// </summary>
    public static class SerializationExtensions
    {
        /// <summary>
        /// Configures the given <see cref="JsonSerializerSettings"/> with default settings.
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
        /// Sets the ContractResolver of the given <see cref="JsonSerializerSettings"/> to a new <see cref="NonDefaultConstructorContractResolver"/>.
        /// </summary>
        /// <param name="settings">The <see cref="JsonSerializerSettings"/> to configure.</param>
        /// <returns>The configured <see cref="JsonSerializerSettings"/>.</returns>
        public static JsonSerializerSettings WithNonDefaultConstructorContractResolver(this JsonSerializerSettings settings)
        {
            settings.ContractResolver = new NonDefaultConstructorContractResolver();
            return settings;
        }

        /// <summary>
        /// Deserializes the JSON to a specified .NET type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="json">The JSON to deserialize.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        public static T FromJson<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json,
                new JsonSerializerSettings().WithNonDefaultConstructorContractResolver())!;
        }

        /// <summary>
        /// Deserializes the JSON to a specified .NET type.
        /// </summary>
        /// <param name="json">The JSON to deserialize.</param>
        /// <param name="type">The type of the object to deserialize to.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        public static object FromJson(this string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type,
                new JsonSerializerSettings().WithNonDefaultConstructorContractResolver())!;
        }

        /// <summary>
        /// Serializes the specified .NET object to a JSON string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// Serializes the specified .NET object to a JSON string and wraps it in a <see cref="StringContent"/> object.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A <see cref="StringContent"/> object containing the JSON string representation of the object.</returns>
        public static StringContent ToJsonStringContent(this object obj)
        {
            return new StringContent(obj.ToJson(), Encoding.UTF8, "application/json");
        }
    }
}
