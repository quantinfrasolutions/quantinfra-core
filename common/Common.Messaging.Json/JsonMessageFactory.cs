using System.Text.Json;
using Common.Utils.Reflection;
using NodaTime;

namespace QuantInfra.Common.Messaging.Json
{
	public class JsonMessageFactory : IMessageFactory
	{
		ITypeResolver _typeResolver;
		JsonSerializerOptions _serializerOptions;


        public JsonMessageFactory(ITypeResolver typeResolver, JsonSerializerOptions serializerOptions)
		{
			_typeResolver = typeResolver;
			_serializerOptions = serializerOptions;
        }


		public string ContentType => "text/json";
		public string SerializeAsString(object? value) => value is null ? "null" : WrapObject(value).GetString();

		public object? Parse(string? payload)
		{
			var msg = CreateReceivedMessage(payload, Instant.MinValue);
			return msg!.GetWrappedObject();
		}

		public IMessage? CreateReceivedMessage(object o, Instant receiveTime)
		{
			var msg = JsonSerializer.Deserialize<JsonMessage>((string)o, _serializerOptions);
			msg.TypeResolver = _typeResolver;
			msg.SerializerOptions = _serializerOptions;
			msg.ReceivedAt = receiveTime;
			return msg;
		}

        public IMessage WrapObject(object o) => new JsonMessage
		{
			Type = o.GetType().FullName,
			Data = JsonSerializer.Serialize(o, _serializerOptions),
			SerializerOptions = _serializerOptions
		};
    }
}

