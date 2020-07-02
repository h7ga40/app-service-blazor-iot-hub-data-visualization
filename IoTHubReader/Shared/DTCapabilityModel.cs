using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

		[JsonPropertyName("@type"), JsonConverter(typeof(DTStringListConverter))]
		public StringList Type { get; set; }

		[JsonPropertyName("description")]
		public DTLocalizable Description { get; set; }

		[JsonPropertyName("displayName"), JsonConverter(typeof(DTLocalizableConverter))]
		public DTLocalizable DisplayName { get; set; }

		[JsonPropertyName("@context"), JsonConverter(typeof(DTStringListConverter))]
		public StringList Context { get; set; }

		[JsonPropertyName("implements")]
		public List<DTInterfaceInstance> Implements { get; set; }
	}

	public class DTInterfaceInstance
	{
		[JsonPropertyName("@id")]
		public string Id { get; set; }

		[JsonPropertyName("@type"), JsonConverter(typeof(DTStringListConverter))]
		public StringList Type { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("description")]
		public DTLocalizable Description { get; set; }

		[JsonPropertyName("displayName"), JsonConverter(typeof(DTLocalizableConverter))]
		public DTLocalizable DisplayName { get; set; }

		[JsonPropertyName("schema"), JsonConverter(typeof(DTSchemaConverter))]
		public DTSchema Schema { get; set; }
	}

	public class DTInterfaceContent
	{
		[JsonPropertyName("@id")]
		public string Id { get; set; }

		[JsonPropertyName("@type"), JsonConverter(typeof(DTStringListConverter))]
		public StringList Type { get; set; }

		[JsonPropertyName("@context"), JsonConverter(typeof(DTStringListConverter))]
		public StringList Context { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("displayName"), JsonConverter(typeof(DTLocalizableConverter))]
		public DTLocalizable DisplayName { get; set; }

		[JsonPropertyName("commandType")]
		public string CommandType { get; set; }

		[JsonPropertyName("schema"), JsonConverter(typeof(DTSchemaConverter))]
		public DTSchema Schema { get; set; }

		[JsonPropertyName("writable")]
		public bool? Writable { get; set; }

		[JsonPropertyName("durable")]
		public bool? Durable { get; set; }

		[JsonPropertyName("request")]
		public DTCommandPayload Request { get; set; }

		[JsonPropertyName("response")]
		public DTCommandPayload Response { get; set; }

		[JsonPropertyName("unit")]
		public string Unit { get; set; }

		[JsonPropertyName("displayUnit")]
		public string DisplayUnit { get; set; }
	}

	public class DTSchema
	{
		[JsonPropertyName("@id")]
		public string Id { get; set; }

		[JsonPropertyName("@type"), JsonConverter(typeof(DTStringListConverter))]
		public StringList Type { get; set; }

		[JsonPropertyName("description")]
		public DTLocalizable Description { get; set; }

		[JsonPropertyName("displayName"), JsonConverter(typeof(DTLocalizableConverter))]
		public DTLocalizable DisplayName { get; set; }

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

		// REAST API用
		[JsonPropertyName("@type"), JsonConverter(typeof(DTStringListConverter))]
		public StringList Type { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("schema"), JsonConverter(typeof(DTSchemaConverter))]
		public DTSchema Schema { get; set; }

		[JsonPropertyName("displayName"), JsonConverter(typeof(DTLocalizableConverter))]
		public DTLocalizable DisplayName { get; set; }

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
		[JsonPropertyName("@id")]
		public string Id { get; set; }

		[JsonPropertyName("@type"), JsonConverter(typeof(DTStringListConverter))]
		public StringList Type { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("enumValue")]
		public long EnumValue { get; set; }

		[JsonPropertyName("displayName"), JsonConverter(typeof(DTLocalizableConverter))]
		public DTLocalizable DisplayName { get; set; }
	}

	public class DTLocalizable : Dictionary<string, string>
	{
		public bool OutputString { get; set; }

		public static implicit operator DTLocalizable(string value)
		{
			return new DTLocalizable { { "en", value } };
		}
	}

	public class DTLocalizableConverter : JsonConverter<DTLocalizable>
	{
		public override bool CanConvert(Type typeToConvert)
		{
			if (typeToConvert == typeof(string))
				return true;
			if (typeToConvert == typeof(DTLocalizable))
				return true;
			return false;
		}

		public override DTLocalizable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String) {
				return new DTLocalizable {
					{ "en", reader.GetString() }
				};
			}

			return JsonSerializer.Deserialize<DTLocalizable>(ref reader, options);
		}

		public override void Write(Utf8JsonWriter writer, DTLocalizable value, JsonSerializerOptions options)
		{
			if (value.OutputString) {
				if (value.Count == 0)
					writer.WriteNullValue();
				else
					writer.WriteStringValue(value.First().Value);
			}
			else if (value.Count == 1 && value.ContainsKey("en"))
				writer.WriteStringValue(value["en"]);
			else
				JsonSerializer.Serialize(writer, value, options);
		}

		public static string GetDisplayName(DTLocalizable localizable, string location = "en")
		{
			if (localizable.Count == 0)
				return "";
			if (localizable.ContainsKey(location))
				return localizable[location];
			return localizable.Values.First();
		}

		public static string GetCodeName(DTLocalizable localizable)
		{
			if (localizable == null || localizable.Count == 0)
				return "";
			if (localizable.ContainsKey("en"))
				return ToLowerUnderbar(localizable["en"]);
			return ToLowerUnderbar(localizable.Values.First());
		}

		public static string MakeDigitalTwinId(string id)
		{
			return ToPascalCase(ToLowerUnderbar(id));
		}

		private static string ToLowerUnderbar(string id)
		{
			int i = 0, len = id.Length;
			char[] temp = new char[len];

			for (char c = id[i]; (i < len) && ((c = id[i]) != '\0'); i++) {
				if (((c >= '0') && (c <= '9')) || (c == '_')
					|| ((c >= 'A') && (c <= 'Z'))
					|| ((c >= 'a') && (c <= 'z'))) {
					temp[i] = c;
				}
				else {
					temp[i] = '_';
				}
			}

			return new string(temp);
		}

		public static string ToPascalCase(string str)
		{
			var ms = Regex.Matches(str, "_([a-z])");
			if (ms.Count == 0)
				return str;

			var result = "";
			int index = 0;
			foreach (Match m in ms) {
				result += str.Substring(index, m.Index - index);
				result += m.Groups[1].Value.ToUpper();
				index = m.Index + m.Length;
			}
			result += str.Substring(index, str.Length - index);

			return result.Substring(0, 1).ToUpper() + result.Substring(1);
		}
	}

	public class StringList : List<string>
	{
		public bool OutputArray { get; set; }

		public static implicit operator StringList(string value)
		{
			return new StringList { value };
		}
	}

	public class DTStringListConverter : JsonConverter<StringList>
	{
		public override bool CanConvert(Type typeToConvert)
		{
			if (typeToConvert == typeof(string))
				return true;
			if (typeToConvert == typeof(StringList))
				return true;
			return false;
		}

		public override StringList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String) {
				return new StringList {
					reader.GetString()
				};
			}

			return JsonSerializer.Deserialize<StringList>(ref reader, options);
		}

		public override void Write(Utf8JsonWriter writer, StringList value, JsonSerializerOptions options)
		{
			if (!value.OutputArray && value.Count == 1)
				writer.WriteStringValue(value[0]);
			else
				JsonSerializer.Serialize(writer, value, options);
		}
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
			if (reader.TokenType == JsonTokenType.String) {
				return new DTSchema {
					Type = reader.GetString()
				};
			}

			return JsonSerializer.Deserialize<DTSchema>(ref reader, options);
		}

		public override void Write(Utf8JsonWriter writer, DTSchema value, JsonSerializerOptions options)
		{
			string type;
			if (value.Type.Count == 1) {
				type = value.Type[0];
				switch (type) {
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
				case "vector":
					writer.WriteStringValue(type);
					break;
				default:
					JsonSerializer.Serialize(writer, value, options);
					break;
				}
			}
			else {
				JsonSerializer.Serialize(writer, value, options);
			}
		}
	}
}
