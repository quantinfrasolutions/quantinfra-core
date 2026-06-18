using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Utils.Reflection;
using NodaTime;

namespace QuantInfra.Common.Messaging.Json
{
    public record JsonMessage : IMessage
	{
		public string Type { get; set; } = default!;
		public string Data { get; set; } = default!;
		[JsonIgnore] internal ITypeResolver TypeResolver { get; set; }
		[JsonIgnore] internal JsonSerializerOptions SerializerOptions { get; set; }
		[JsonIgnore] public Instant ReceivedAt { get; set; }
		
		public string GetString() => JsonSerializer.Serialize(this, SerializerOptions);

		public byte[] GetBytes() => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, SerializerOptions));

        public object? GetWrappedObject() => JsonSerializer.Deserialize(
			Data,
			TypeResolver.ResolveType(Type)!,
			SerializerOptions
		);
    }
}

