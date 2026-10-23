using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShoppingCart.Domain.Common;

namespace ShoppingCart.Application.Common.Serialization;

/// <summary>
/// Serializes strongly typed ids (<see cref="IStronglyTypedId"/>) as plain Guids and reads them back
/// through their validating <c>Of(Guid)</c> factory.
/// </summary>
public sealed class StronglyTypedIdJsonConverterFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter> Converters = new();

    public override bool CanConvert(Type typeToConvert) =>
        typeof(IStronglyTypedId).IsAssignableFrom(typeToConvert) && !typeToConvert.IsAbstract;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        Converters.GetOrAdd(typeToConvert, static type =>
            (JsonConverter)Activator.CreateInstance(typeof(StronglyTypedIdJsonConverter<>).MakeGenericType(type))!);

    private sealed class StronglyTypedIdJsonConverter<T> : JsonConverter<T> where T : IStronglyTypedId
    {
        private static readonly Func<Guid, T> Factory = CreateFactory();

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Factory(reader.GetGuid());

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);

        private static Func<Guid, T> CreateFactory()
        {
            var method = typeof(T).GetMethod("Of", BindingFlags.Public | BindingFlags.Static, [typeof(Guid)])
                ?? throw new InvalidOperationException($"{typeof(T).Name} must declare a public static Of(Guid) factory.");
            return method.CreateDelegate<Func<Guid, T>>();
        }
    }
}
