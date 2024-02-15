using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;
using System.Reflection;

namespace GhostLyzer.Core.EventStoreDB.Serialization
{
    /// <summary>
    /// Provides methods for managing JsonObjectContract instances with non-default constructors.
    /// </summary>
    public static class JsonObjectContractProvider
    {
        private static readonly Type ConstructorAttributeType = typeof(JsonConstructorAttribute);
        private static readonly ConcurrentDictionary<string, JsonObjectContract> Constructors = new();

        /// <summary>
        /// Gets a JsonObjectContract for the specified type, using a non-default constructor if one is available.
        /// </summary>
        /// <param name="contract">The original JsonObjectContract.</param>
        /// <param name="objectType">The type of the object.</param>
        /// <param name="createConstructorParameters">A function that creates a list of JsonProperty instances for the constructor parameters.</param>
        /// <returns>A JsonObjectContract that uses a non-default constructor, if one is available.</returns>
        public static JsonObjectContract UsingNonDefaultConstructor(
            JsonObjectContract contract,
            Type objectType,
            Func<ConstructorInfo, JsonPropertyCollection, IList<JsonProperty>> createConstructorParameters) =>
            Constructors.GetOrAdd(objectType.AssemblyQualifiedName!, _ =>
            {
                var nonDefaultConstructor = GetNonDefaultConstructor(objectType);

                if (nonDefaultConstructor == null) return contract;

                contract.OverrideCreator = GetObjectConstructor(nonDefaultConstructor);
                contract.CreatorParameters.Clear();

                foreach (var constructorParameter in createConstructorParameters(nonDefaultConstructor, contract.Properties))
                {
                    contract.CreatorParameters.Add(constructorParameter);
                }

                return contract;
            });

        /// <summary>
        /// Gets a delegate that invokes the specified method.
        /// </summary>
        /// <param name="method">The method to invoke.</param>
        /// <returns>A delegate that invokes the specified method.</returns>
        private static ObjectConstructor<object> GetObjectConstructor(MethodBase method)
        {
            // Check if the method is null and throw an exception if it is
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            // Try to cast the method to a ConstructorInfo
            var c = method as ConstructorInfo;

            // If the method is not a constructor, create a delegate that invokes the method
            if (c == null)
            {
                if (!method.IsStatic)
                {
                    throw new ArgumentException("The provided method must be static if it is not a constructor.", nameof(method));
                }

                return a => method.Invoke(null, a)!;
            }

            // If the constructor does not have any parameters, create a delegate that invokes the constructor without any arguments
            if (!c.GetParameters().Any())
            {
                return _ => c.Invoke(Array.Empty<object?>());
            }

            // Otherwise, create a delegate that invokes the constructor with the provided arguments
            return a => c.Invoke(a);
        }

        /// <summary>
        /// Gets a non-default constructor for the specified type, if one is available.
        /// </summary>
        /// <param name="objectType">The type of the object.</param>
        /// <returns>A non-default constructor for the specified type, if one is available; otherwise, null.</returns>
        private static ConstructorInfo? GetNonDefaultConstructor(Type objectType)
        {
            // Check if the objectType is null and throw an exception if it is
            ArgumentNullException.ThrowIfNull(objectType);

            // If the objectType is a primitive type or an enum, return null
            // as these types don't have constructors
            if (objectType.IsPrimitive || objectType.IsEnum)
                return null;

            // Try to get a constructor with a specific attribute
            // If that fails, get the most specific constructor (the one with the most parameters)
            return GetAttributeConstructor(objectType)
                ?? GetTheMostSpecificConstructor(objectType);
        }

        /// <summary>
        /// Gets a constructor for the specified type that has a JsonConstructorAttribute, if one is available.
        /// </summary>
        /// <param name="objectType">The type of the object.</param>
        /// <returns>A constructor for the specified type that has a JsonConstructorAttribute, if one is available; otherwise, null.</returns>
        private static ConstructorInfo? GetAttributeConstructor(Type objectType)
        {
            // If the objectType is a primitive type or an enum, return null
            // as these types don't have constructors
            if (objectType.IsPrimitive || objectType.IsEnum) return null;

            // Get all constructors of the objectType, including non-public ones
            // Filter the constructors to only include those that have the ConstructorAttributeType attribute
            var constructors = objectType
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(c => c.GetCustomAttributes().Any(a => a.GetType() == ConstructorAttributeType));

            // If there are more than one constructors with the ConstructorAttributeType attribute,
            // throw an exception as this is not allowed
            if (constructors.Count() > 1)
            {
                throw new JsonException($"Multiple constructors with a {ConstructorAttributeType.Name}.");
            }

            // Return the first constructor that has the ConstructorAttributeType attribute,
            // or null if there are no such constructors
            return constructors.FirstOrDefault();
        }

        /// <summary>
        /// Gets the most specific constructor for the specified type.
        /// </summary>
        /// <param name="objectType">The type of the object.</param>
        /// <returns>The most specific constructor for the specified type.</returns>
        private static ConstructorInfo? GetTheMostSpecificConstructor(Type objectType)
        {
            // Check if the objectType is null and throw an exception if it is
            ArgumentNullException.ThrowIfNull(objectType);

            // Get all constructors of the objectType, including non-public ones
            // Order them by the number of parameters in descending order
            // Return the first constructor in the ordered list (i.e., the one with the most parameters)
            return objectType
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderByDescending(e => e.GetParameters().Length).FirstOrDefault();
        }
    }
}
