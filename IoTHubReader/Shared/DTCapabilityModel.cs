using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IoTHubReader.Shared
{
	public enum DTInterfaceType
	{
		None,
		Telemetry,
		Property,
		Command,
	}

	public class DTCapabilityModel
	{
		[JsonPropertyName("@id")]
		public string Id { get; set; }

		[JsonPropertyName("@type")]
		public string Type { get; set; }

		[JsonPropertyName("description")]
		public Dictionary<string, string> Description { get; set; }

		[JsonPropertyName("displayName")]
		public Dictionary<string, string> DisplayName { get; set; }

		[JsonPropertyName("@context")]
		public string Context { get; set; }

		[JsonPropertyName("implements")]
		public List<DTInterfaceInstance> Implements { get; set; }
	}

	public class DTInterfaceInstance
	{
		[JsonPropertyName("@id")]
		public string Id { get; set; }

		[JsonPropertyName("@type")]
		public string Type { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("description")]
		public Dictionary<string, string> Description { get; set; }

		[JsonPropertyName("displayName")]
		public Dictionary<string, string> DisplayName { get; set; }

		[JsonPropertyName("schema"), JsonConverter(typeof(DTSchemaConverter))]
		public DTSchema Schema { get; set; }
	}

	public class DTInterfaceContent
	{
		[JsonPropertyName("@id")]
		public string Id { get; set; }

		[JsonPropertyName("@type")]
		public string Type { get; set; }

		[JsonPropertyName("@context")]
		public string Context { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("displayName")]
		public Dictionary<string, string> DisplayName { get; set; }

		[JsonPropertyName("commandType")]
		public string CommandType { get; set; }

		[JsonPropertyName("schema"), JsonConverter(typeof(DTSchemaConverter))]
		public DTSchema Schema { get; set; }

		[JsonPropertyName("writable")]
		public bool Writable { get; set; }

		[JsonPropertyName("request")]
		public DTCommandPayload Request { get; set; }

		[JsonPropertyName("response")]
		public DTCommandPayload Response { get; set; }

		[JsonPropertyName("displayUnit")]
		public string DisplayUnit { get; set; }
	}

	public class DTSchema
	{
		[JsonPropertyName("@id")]
		public string Id { get; set; }

		[JsonPropertyName("@type")]
		public string Type { get; set; }

		[JsonPropertyName("description")]
		public Dictionary<string, string> Description { get; set; }

		[JsonPropertyName("displayName")]
		public Dictionary<string, string> DisplayName { get; set; }

		[JsonPropertyName("fields")]
		public List<DTField> Fields { get; set; }

		[JsonPropertyName("valueSchema")]
		public string ValueSchema { get; set; }

		[JsonPropertyName("enumValues")]
		public List<DTEnumValue> EnumValues { get; set; }

		[JsonPropertyName("contents")]
		public List<DTInterfaceContent> Contents { get; set; }
	}

	public class DTCommandPayload
	{
		[JsonPropertyName("@id")]
		public string Id { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("schema"), JsonConverter(typeof(DTSchemaConverter))]
		public DTSchema Schema { get; set; }

		[JsonPropertyName("displayName")]
		public Dictionary<string, string> DisplayName { get; set; }

		[JsonPropertyName("displayUnit")]
		public string DisplayUnit { get; set; }
	}

	public class DTField
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("schema"), JsonConverter(typeof(DTSchemaConverter))]
		public DTSchema Schema { get; set; }
	}

	public class DTEnumValue
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("enumValue")]
		public long EnumValue { get; set; }

		[JsonPropertyName("displayName")]
		public Dictionary<string, string> DisplayName { get; set; }
	}

	public class DTSchemaConverter : JsonConverter<DTSchema>
	{
		public override bool CanConvert(Type typeToConvert)
		{
			if (typeToConvert == typeof(string))
				return true;
			if (typeToConvert == typeof(DTSchema))
				return true;
			return false;
		}

		public override DTSchema Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (typeToConvert == typeof(string)) {
				return new DTSchema {
					Type = reader.GetString()
				};
			}

			return JsonSerializer.Deserialize<DTSchema>(ref reader, options);
		}

		public override void Write(Utf8JsonWriter writer, DTSchema value, JsonSerializerOptions options)
		{
			switch (value.Type) {
			case "boolean":
			case "date":
			case "datetime":
			case "double":
			case "duration":
			case "float":
			case "integer":
			case "long":
			case "string":
			case "time":
				writer.WriteStringValue(value.Type);
				break;
			default:
				JsonSerializer.Serialize(writer, value, options);
				break;
			}
		}
	}
}
