using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using IoTHubReader.Shared;

namespace IoTHubReader.Client.Pages
{
	partial class Counter
	{
		void ReadELDeviceDescription(byte[] data)
		{
			var options = new JsonReaderOptions {
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip
			};
			var reader = new Utf8JsonReader(data, options);

			DeviceDescription = new ELDeviceDescription();
			DeviceDescription.WalkJson(ref reader);
		}

		public static void WalkProperties(List<DTInterfaceContent> dtifs, ELDevice device)
		{
			var deviceName = make_digital_twin_id(device.En);

			if (device.Properties != null) {
				foreach (var property in device.Properties) {
					MakeDTInterface(dtifs, 0, deviceName, GetAccessValue(property),
						property.Ja, property.En, property.Data);

					if (property.OneOf != null) {
						foreach (var one in property.OneOf) {
							MakeDTInterface(dtifs, 0, deviceName, GetAccessValue(one),
									one.Ja, one.En, one.Data);
						}
					}
				}
			}
		}

		ELDeviceDescription DeviceDescription;

		public static string MakeDeviceTemplate(ELDevice device)
		{
			var dtifs = new List<DTInterfaceContent>();
			var deviceName = make_digital_twin_id(device.En);

			foreach (var property in device.Properties) {
				MakeDTInterface(dtifs, 0, deviceName, GetAccessValue(property),
					property.Ja, property.En, property.Data);

				if (property.OneOf != null) {
					foreach (var one in property.OneOf) {
						MakeDTInterface(dtifs, 0, deviceName, GetAccessValue(one),
							one.Ja, one.En, one.Data);
					}
				}
			}

			var deviceTemplate = new DTCapabilityModel {
				Id = $"urn:EchonetLite:{deviceName}CM:1",
				Type = "CapabilityModel",
				Context = "http://azureiot.com/v1/contexts/IoTModel.json",
				DisplayName = new Dictionary<string, string> {
					{ "en", device.En },
					{ "ja", device.Ja },
				},
				Implements = new List<DTInterfaceInstance> {
					new DTInterfaceInstance {
						Id = $"urn:EchonetLite:{deviceName}:ver_1:1",
						Type = "InterfaceInstance",
						Name = deviceName,
						DisplayName = new Dictionary<string, string> {
							{ "en", "Interface" },
							{ "ja", "インターフェイス" }
						},
						Schema = new DTSchema {
							Id = $"urn:EchonetLite:{deviceName}:1",
							Type = "Interface",
							DisplayName = new Dictionary<string, string> {
								{ "en", "Interface" },
								{ "ja", "インターフェイス" }
							},
							Contents = dtifs
						}
					}
				}
			};

			var option = new JsonSerializerOptions {
				WriteIndented = true,
				IgnoreNullValues = true,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			};
			return JsonSerializer.Serialize(deviceTemplate, option);
		}

		public static uint GetAccessValue(ELProperty property)
		{
			return GetAccessValue(property.Get, property.Set, property.Inf);
		}

		public static uint GetAccessValue(ELAccessRule get, ELAccessRule set, ELAccessRule inf)
		{
			return (((uint)inf & 0xF) << 8) | (((uint)set & 0xF) << 4) | (((uint)get & 0xF) << 0);
		}

		internal static string make_digital_twin_id(string id)
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

			return ToPascalCase(new string(temp));
		}

		private static string ToPascalCase(string str)
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

			return result;
		}

		public static void MakeDTInterface(List<DTInterfaceContent> dtifs, int index,
			string deviceName, uint accessValue,
			string propertyNameJa, string propertyNameEn, ELDefinition dataInfo)
		{
			DTInterfaceType if_type;
			bool writable = false;

			if (accessValue == 0) {
				return;
			}
			else if (accessValue == GetAccessValue(ELAccessRule.NA, ELAccessRule.Optional, ELAccessRule.NA)) {
				// GETなし
				if_type = DTInterfaceType.Command;
			}
			else if (accessValue == GetAccessValue(ELAccessRule.NA, ELAccessRule.Required, ELAccessRule.Optional)) {
				// GETなし
				if_type = DTInterfaceType.Command;
			}
			else if ((accessValue == GetAccessValue(ELAccessRule.Optional, ELAccessRule.NA, ELAccessRule.Required))
				|| (accessValue == GetAccessValue(ELAccessRule.Required, ELAccessRule.NA, ELAccessRule.Required))
				|| (accessValue == GetAccessValue(ELAccessRule.ByCase, ELAccessRule.NA, ELAccessRule.Required))) {
				// SETなし
				if_type = DTInterfaceType.Telemetry;
			}
			else if ((accessValue == GetAccessValue(ELAccessRule.Optional, ELAccessRule.NA, ELAccessRule.Optional))
				|| (accessValue == GetAccessValue(ELAccessRule.Required, ELAccessRule.NA, ELAccessRule.Optional))
				|| (accessValue == GetAccessValue(ELAccessRule.ByCase, ELAccessRule.NA, ELAccessRule.Optional))) {
				// SETなし
				if_type = (dataInfo.Type == ELDataType.State) ? DTInterfaceType.Telemetry : DTInterfaceType.Property;
			}
			else if (accessValue == GetAccessValue(ELAccessRule.Required, ELAccessRule.Required, ELAccessRule.NA)) {
				// GET/SET
				if_type = DTInterfaceType.Property;
				writable = true;
			}
			else if ((accessValue == GetAccessValue(ELAccessRule.Optional, ELAccessRule.Optional, ELAccessRule.Required))
				|| (accessValue == GetAccessValue(ELAccessRule.Required, ELAccessRule.Optional, ELAccessRule.Required))
				|| (accessValue == GetAccessValue(ELAccessRule.Required, ELAccessRule.Required, ELAccessRule.Required))
				|| (accessValue == GetAccessValue(ELAccessRule.ByCase, ELAccessRule.ByCase, ELAccessRule.Required))) {
				// GET/SET
				if_type = DTInterfaceType.Property;
				writable = true;
			}
			else if (accessValue == GetAccessValue(ELAccessRule.Optional, ELAccessRule.Optional, ELAccessRule.Optional)) {
				// GET/SET
				if_type = DTInterfaceType.Property;
				writable = true;
			}
			else if ((accessValue == GetAccessValue(ELAccessRule.Required, ELAccessRule.Optional, ELAccessRule.Optional))
				|| (accessValue == GetAccessValue(ELAccessRule.ByCase, ELAccessRule.Optional, ELAccessRule.Optional))) {
				// GET/SET
				if_type = DTInterfaceType.Property;
				writable = true;
			}
			else if ((accessValue == GetAccessValue(ELAccessRule.Required, ELAccessRule.Required, ELAccessRule.Optional))
				|| (accessValue == GetAccessValue(ELAccessRule.ByCase, ELAccessRule.ByCase, ELAccessRule.Optional))) {
				// GET/SET
				if_type = DTInterfaceType.Property;
				writable = true;
			}
			else {
				if_type = DTInterfaceType.None;
				System.Diagnostics.Debugger.Break();
			}

			if ((if_type == DTInterfaceType.Command) && (dataInfo.Type == ELDataType.State)) {
				foreach (var edt in dataInfo.EdtInfos) {
					DTInterfaceContent command = new DTInterfaceContent();

					string name;
					if (edt.StateEn != null) {
						name = make_digital_twin_id(edt.StateEn);
					}
					else {
						name = $"edt{edt.Edt:2X}";
					}

					command.Type = "Command";
					command.Context = "http://azureiot.com/v1/contexts/IoTModel.json";
					command.Name = name;
					command.DisplayName = new Dictionary<string, string>();
					command.DisplayName["en"] = edt.StateEn;
					command.DisplayName["ja"] = edt.StateJa;

					command.CommandType = "synchronous";

					dtifs.Add(command);
				}
			}
			else {
				DTInterfaceContent ifcnt = new DTInterfaceContent();

				var name = make_digital_twin_id(propertyNameEn);

				if (index == 0) {
					if (String.IsNullOrEmpty(deviceName))
						ifcnt.Id = $"urn:EchonetLite:{name}:1";
					else
						ifcnt.Id = $"urn:EchonetLite:{deviceName}:{name}:1";
				}
				else {
					if (String.IsNullOrEmpty(deviceName))
						ifcnt.Id = $"urn:EchonetLite:{name}{index + 1}:1";
					else
						ifcnt.Id = $"urn:EchonetLite:{deviceName}:{name}{index + 1}:1";
				}

				switch (if_type) {
				case DTInterfaceType.Command:
					ifcnt.Type = "Command";
					break;
				case DTInterfaceType.Telemetry:
					ifcnt.Type = "Telemetry";
					break;
				case DTInterfaceType.Property:
					ifcnt.Type = "Property";
					break;
				default:
					System.Diagnostics.Debugger.Break();
					return;
				}

				ifcnt.Context = "http://azureiot.com/v1/contexts/IoTModel.json";
				ifcnt.Name = name;

				switch (if_type) {
				case DTInterfaceType.Telemetry:
				case DTInterfaceType.Property:
					DTSchema request = make_schema(dataInfo);
					ifcnt.Schema = request;
					break;
				}

				ifcnt.DisplayName = new Dictionary<string, string>();
				ifcnt.DisplayName["en"] = propertyNameEn;
				ifcnt.DisplayName["ja"] = propertyNameJa;

				switch (if_type) {
				case DTInterfaceType.Command: {
					ifcnt.CommandType = "synchronous";
					var request = make_command_payload(propertyNameJa, propertyNameEn, dataInfo);
					ifcnt.Request = request;
					var response = make_command_payload(propertyNameJa, propertyNameEn, dataInfo);
					ifcnt.Response = response;
					break;
				}
				case DTInterfaceType.Telemetry:
					if (dataInfo.Unit != null) {
						ifcnt.DisplayUnit = dataInfo.Unit;
					}
					break;
				case DTInterfaceType.Property:
					if (dataInfo.Unit != null) {
						ifcnt.DisplayUnit = dataInfo.Unit;
					}
					ifcnt.Writable = writable;
					break;
				default:
					System.Diagnostics.Debugger.Break();
					return;
				}

				dtifs.Add(ifcnt);
			}
		}

		static DTSchema make_schema(ELDefinition dataInfo)
		{
			var schema = new DTSchema();

			switch (dataInfo.Type) {
			case ELDataType.None:
				if (dataInfo.Reference != null) {
					return make_schema(dataInfo.Reference);
				}
				break;
			case ELDataType.State: {
				schema.Type = "Enum";
				schema.ValueSchema = "integer";

				schema.EnumValues = new List<DTEnumValue>();

				foreach (var edt in dataInfo.EdtInfos) {
					var enumValue = new DTEnumValue();
					schema.EnumValues.Add(enumValue);

					string name;
					if (edt.StateEn != null) {
						name = make_digital_twin_id(edt.StateEn);
					}
					else {
						name = $"edt{edt.Edt:x}";
					}
					enumValue.Name = name;

					enumValue.EnumValue = edt.Edt;

					enumValue.DisplayName = new Dictionary<string, string>();
					enumValue.DisplayName["en"] = edt.StateEn;
					enumValue.DisplayName["ja"] = edt.StateJa;
				}
				break;
			}
			case ELDataType.Object: {
				schema.Type = "Object";
				schema.Fields = new List<DTField>();

				foreach (var dataInfo2 in dataInfo.DataInfos) {
					var field = new DTField();
					schema.Fields.Add(field);

					field.Name = dataInfo2.Name;
					var schema2 = make_schema(dataInfo2);
					field.Schema = schema2;
				}
				break;
			}
			case ELDataType.DateTime: {
				schema.Type = "datetime";
				break;
			}
			case ELDataType.Time: {
				schema.Type = "time";
				break;
			}
			case ELDataType.Raw: {
				schema.Type = "String";
				break;
			}
			case ELDataType.Array: {
				schema.Type = "Array";
				break;
			}
			case ELDataType.Bitmap: {
				schema.Type = "Object";
				schema.Fields = new List<DTField>();

				foreach (var bitmapInfo in dataInfo.BitmapInfos) {
					var field = new DTField();
					schema.Fields.Add(field);

					field.Name = bitmapInfo.Name;
					var schema2 = make_schema(bitmapInfo.Value);
					field.Schema = schema2;
				}
				break;
			}
			case ELDataType.Level: {
				schema.Type = "integer";
				break;
			}
			case ELDataType.Number: {
				switch (dataInfo.NumFormat) {
				case ELNumberFormat.Int8:
				case ELNumberFormat.Int16:
				case ELNumberFormat.Int32:
				case ELNumberFormat.Uint8:
				case ELNumberFormat.Uint16:
					schema.Type = "integer";
					break;
				case ELNumberFormat.Uint32:
					schema.Type = "long";
					break;
				default:
					schema = null;
					System.Diagnostics.Debugger.Break();
					break;
				}
				break;
			}
			case ELDataType.NumericValue: {
				schema.Type = "integer";
				break;
			}
			case ELDataType.OneOf: {
				schema.Type = "Object";
				schema.Fields = new List<DTField>();

				foreach (var dataInfo2 in dataInfo.DataInfos) {
					DTField field = new DTField();
					schema.Fields.Add(field);

					field.Name = dataInfo2.Name;
					var schema2 = make_schema(dataInfo2);
					field.Schema = schema2;
				}
				break;
			}
			default:
				schema = null;
				System.Diagnostics.Debugger.Break();
				break;
			}

			return schema;
		}

		static DTCommandPayload make_command_payload(string propertyNameJa, string propertyNameEn, ELDefinition dataInfo)
		{
			var command = new DTCommandPayload();

			var name = make_digital_twin_id(propertyNameEn);

			var temp = $"urn:EchonetLite:{name}:1";

			command.Id = temp;

			command.Name = name;

			var request = make_schema(dataInfo);
			command.Schema = request;

			command.DisplayName = new Dictionary<string, string>();
			command.DisplayName["en"] = propertyNameEn;
			command.DisplayName["ja"] = propertyNameJa;

			command.DisplayUnit = dataInfo.Unit;

			return command;
		}
	}

	enum DTInterfaceType
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
		public string Context { get; internal set; }

		[JsonPropertyName("implements")]
		public List<DTInterfaceInstance> Implements { get; internal set; }
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
		public DTSchema Schema { get; internal set; }
	}

	public class DTInterfaceContent
	{
		[JsonPropertyName("@id")]
		public string Id { get; internal set; }

		[JsonPropertyName("@type")]
		public string Type { get; internal set; }

		[JsonPropertyName("@context")]
		public string Context { get; internal set; }

		[JsonPropertyName("name")]
		public string Name { get; internal set; }

		[JsonPropertyName("displayName")]
		public Dictionary<string, string> DisplayName { get; internal set; }

		[JsonPropertyName("commandType")]
		public string CommandType { get; internal set; }

		[JsonPropertyName("schema"), JsonConverter(typeof(DTSchemaConverter))]
		public DTSchema Schema { get; internal set; }

		[JsonPropertyName("writable")]
		public bool Writable { get; internal set; }

		[JsonPropertyName("request")]
		public DTCommandPayload Request { get; internal set; }

		[JsonPropertyName("response")]
		public DTCommandPayload Response { get; internal set; }

		[JsonPropertyName("displayUnit")]
		public string DisplayUnit { get; internal set; }
	}

	public class DTSchema
	{
		[JsonPropertyName("@id")]
		public string Id { get; set; }

		[JsonPropertyName("@type")]
		public string Type { get; internal set; }

		[JsonPropertyName("description")]
		public Dictionary<string, string> Description { get; set; }

		[JsonPropertyName("displayName")]
		public Dictionary<string, string> DisplayName { get; set; }

		[JsonPropertyName("fields")]
		public List<DTField> Fields { get; internal set; }

		[JsonPropertyName("valueSchema")]
		public string ValueSchema { get; internal set; }

		[JsonPropertyName("enumValues")]
		public List<DTEnumValue> EnumValues { get; internal set; }

		[JsonPropertyName("contents")]
		public List<DTInterfaceContent> Contents { get; internal set; }
	}

	public class DTCommandPayload
	{
		[JsonPropertyName("@id")]
		public string Id { get; internal set; }

		[JsonPropertyName("name")]
		public string Name { get; internal set; }

		[JsonPropertyName("schema"), JsonConverter(typeof(DTSchemaConverter))]
		public DTSchema Schema { get; internal set; }

		[JsonPropertyName("displayName")]
		public Dictionary<string, string> DisplayName { get; internal set; }

		[JsonPropertyName("displayUnit")]
		public string DisplayUnit { get; internal set; }
	}

	public class DTField
	{
		[JsonPropertyName("name")]
		public string Name { get; internal set; }

		[JsonPropertyName("schema"), JsonConverter(typeof(DTSchemaConverter))]
		public DTSchema Schema { get; internal set; }
	}

	public class DTEnumValue
	{
		[JsonPropertyName("name")]
		public string Name { get; internal set; }

		[JsonPropertyName("enumValue")]
		public long EnumValue { get; internal set; }

		[JsonPropertyName("displayName")]
		public Dictionary<string, string> DisplayName { get; internal set; }
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
