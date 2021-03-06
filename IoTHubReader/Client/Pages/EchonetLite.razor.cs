﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using IoTHubReader.Shared;

namespace IoTHubReader.Client.Pages
{
	partial class EchonetLite
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
			var deviceName = DTLocalizableConverter.MakeDigitalTwinId(device.En);

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

		public static DTInterface MakeInterface(ELDevice device)
		{
			var dtifs = new List<DTInterfaceContent>();
			var deviceName = DTLocalizableConverter.MakeDigitalTwinId(device.En);

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

			return new DTInterface {
				Id = $"dtmi:EchonetLite:{deviceName};1",
				Type = "Interface",
				Context = "dtmi:dtdl:context;2",
				DisplayName = new DTLocalizable {
					{ "en", device.En },
					{ "ja", device.Ja },
				},
				Contents = dtifs
			};
		}

		public static uint GetAccessValue(ELProperty property)
		{
			return GetAccessValue(property.Get, property.Set, property.Inf);
		}

		public static uint GetAccessValue(ELAccessRule get, ELAccessRule set, ELAccessRule inf)
		{
			return (((uint)inf & 0xF) << 8) | (((uint)set & 0xF) << 4) | (((uint)get & 0xF) << 0);
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
						name = DTLocalizableConverter.MakeDigitalTwinId(edt.StateEn);
					}
					else {
						name = $"edt{edt.Edt:2X}";
					}

					command.Type = "Command";
					command.Name = name;
					command.DisplayName = new DTLocalizable {
						{ "en", edt.StateEn },
						{ "ja", edt.StateJa }
					};

					command.CommandType = "synchronous";

					dtifs.Add(command);
				}
			}
			else {
				DTInterfaceContent ifcnt = new DTInterfaceContent();

				var name = DTLocalizableConverter.MakeDigitalTwinId(propertyNameEn);

				if (index == 0) {
					if (String.IsNullOrEmpty(deviceName))
						ifcnt.Id = $"dtmi:EchonetLite:{name};1";
					else
						ifcnt.Id = $"dtmi:EchonetLite:{deviceName}:{name};1";
				}
				else {
					if (String.IsNullOrEmpty(deviceName))
						ifcnt.Id = $"dtmi:EchonetLite:{name}{index + 1};1";
					else
						ifcnt.Id = $"dtmi:EchonetLite:{deviceName}:{name}{index + 1};1";
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

				ifcnt.Name = name;

				switch (if_type) {
				case DTInterfaceType.Telemetry:
				case DTInterfaceType.Property:
					DTSchema request = make_schema(dataInfo);
					ifcnt.Schema = request;
					break;
				}

				ifcnt.DisplayName = new DTLocalizable {
					{ "en", propertyNameEn },
					{ "ja", propertyNameJa }
				};

				switch (if_type) {
				case DTInterfaceType.Command: {
					ifcnt.CommandType = "synchronous";
					var request = make_command_payload(propertyNameJa, propertyNameEn, dataInfo, true);
					ifcnt.Request = request;
					var response = make_command_payload(propertyNameJa, propertyNameEn, dataInfo, false);
					ifcnt.Response = response;
					break;
				}
				case DTInterfaceType.Telemetry:
					break;
				case DTInterfaceType.Property:
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
						name = DTLocalizableConverter.MakeDigitalTwinId(edt.StateEn);
					}
					else {
						name = $"edt{edt.Edt:x}";
					}
					enumValue.Name = name;

					enumValue.EnumValue = edt.Edt;

					enumValue.DisplayName = new DTLocalizable {
						{ "en", edt.StateEn },
						{ "ja", edt.StateJa }
					};
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
				schema.Type = "dateTime";
				break;
			}
			case ELDataType.Time: {
				schema.Type = "time";
				break;
			}
			case ELDataType.Raw: {
				schema.Type = "string";
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

		static DTCommandPayload make_command_payload(string propertyNameJa, string propertyNameEn, ELDefinition dataInfo, bool request)
		{
			var command = new DTCommandPayload();

			var name = DTLocalizableConverter.MakeDigitalTwinId(propertyNameEn);

			var temp = $"dtmi:EchonetLite:{name}:{(request ? "Request" : "Response")};1";

			command.Id = temp;

			command.Name = name;

			var schema = make_schema(dataInfo);
			command.Schema = schema;

			command.DisplayName = new DTLocalizable {
				{ "en", propertyNameEn },
				{ "ja", propertyNameJa }
			};

			return command;
		}

		private DTInterface ModifyForRestApi(DTInterface src)
		{
			DTInterface dst = new DTInterface();

			dst.Id = src.Id;

			if (src.Type != null) {
				dst.Type = new StringList { OutputArray = true };
				dst.Type.AddRange(src.Type);
			}

			if (src.Description != null) {
				dst.Description = new DTLocalizable { OutputString = true };
				foreach (var kvp in src.Description)
					dst.Description.Add(kvp.Key, kvp.Value);
			}

			if (src.DisplayName != null) {
				dst.DisplayName = new DTLocalizable { OutputString = true };
				foreach (var kvp in src.DisplayName)
					dst.DisplayName.Add(kvp.Key, kvp.Value);
			}

			//if (src.Context != null) {
			//	dst.Context = new StringList { OutputArray = true };
			//	dst.Context.AddRange(src.Context);
			//}

			if (src.Contents != null) {
				dst.Contents = new List<DTInterfaceContent>();
				foreach (var implement in src.Contents) {
					dst.Contents.Add(ModifyForRestApi(implement));
				}
			}

			return dst;
		}

		private DTSchema ModifyForRestApi(DTSchema src)
		{
			DTSchema dst = new DTSchema();

			dst.Id = src.Id;

			if (src.Type != null) {
				dst.Type = new StringList { OutputArray = true };
				dst.Type.AddRange(src.Type);
			}

			if (src.Description != null) {
				dst.Description = new DTLocalizable { OutputString = true };
				foreach (var kvp in src.Description)
					dst.Description.Add(kvp.Key, kvp.Value);
			}

			if (src.DisplayName != null) {
				dst.DisplayName = new DTLocalizable { OutputString = true };
				foreach (var kvp in src.DisplayName)
					dst.DisplayName.Add(kvp.Key, kvp.Value);
			}

			dst.ValueSchema = src.ValueSchema;

			if (src.Fields != null) {
				dst.Fields = new List<DTField>();
				foreach (var field in src.Fields) {
					dst.Fields.Add(ModifyForRestApi(field));
				}
			}

			if (src.EnumValues != null) {
				dst.EnumValues = new List<DTEnumValue>();
				foreach (var enumValue in src.EnumValues) {
					dst.EnumValues.Add(ModifyForRestApi(enumValue));
				}
			}

			if (src.Contents != null) {
				dst.Contents = new List<DTInterfaceContent>();
				foreach (var content in src.Contents) {
					dst.Contents.Add(ModifyForRestApi(content));
				}
			}

			return dst;
		}

		private DTField ModifyForRestApi(DTField src)
		{
			DTField dst = new DTField();

			dst.Name = src.Name;

			if (src.Schema != null) {
				dst.Schema = ModifyForRestApi(src.Schema);
			}

			return dst;
		}

		private DTEnumValue ModifyForRestApi(DTEnumValue src)
		{
			DTEnumValue dst = new DTEnumValue();

			dst.Id = src.Id;

			if (src.Type != null) {
				dst.Type = new StringList { OutputArray = true };
				dst.Type.AddRange(src.Type);
			}

			dst.Name = src.Name;

			dst.EnumValue = src.EnumValue;

			if (src.DisplayName != null) {
				dst.DisplayName = new DTLocalizable { OutputString = true };
				foreach (var kvp in src.DisplayName)
					dst.DisplayName.Add(kvp.Key, kvp.Value);
			}

			return dst;
		}

		private DTInterfaceContent ModifyForRestApi(DTInterfaceContent src)
		{
			DTInterfaceContent dst = new DTInterfaceContent();

			dst.Id = src.Id;

			if (src.Type != null) {
				dst.Type = new StringList { OutputArray = true };
				dst.Type.AddRange(src.Type);
			}

			//if (src.Context != null) {
			//	dst.Context = new StringList { OutputArray = true };
			//	dst.Context.AddRange(src.Context);
			//}

			dst.Name = src.Name;

			if (src.Description != null) {
				dst.Description = new DTLocalizable { OutputString = true };
				foreach (var kvp in src.Description)
					dst.Description.Add(kvp.Key, kvp.Value);
			}

			if (src.DisplayName != null) {
				dst.DisplayName = new DTLocalizable { OutputString = true };
				foreach (var kvp in src.DisplayName)
					dst.DisplayName.Add(kvp.Key, kvp.Value);
			}

			dst.CommandType = src.CommandType;

			if (src.Schema != null) {
				dst.Schema = ModifyForRestApi(src.Schema);
			}

			dst.Writable = src.Writable;

			dst.Durable = src.Durable;

			if (src.Request != null) {
				dst.Request = ModifyForRestApi(src.Request);
			}

			if (src.Response != null) {
				dst.Response = ModifyForRestApi(src.Response);
			}

			dst.Unit = src.Unit;

			return dst;
		}

		private DTCommandPayload ModifyForRestApi(DTCommandPayload src)
		{
			DTCommandPayload dst = new DTCommandPayload();

			dst.Id = src.Id;

			dst.Name = src.Name;

			if (src.Schema != null) {
				dst.Schema = ModifyForRestApi(src.Schema);

				// CommandPayloadはREST APIでは@typeが必要
				dst.Type = new StringList { OutputArray = true };
				dst.Type.Add("SchemaField");
			}

			if (src.DisplayName != null) {
				dst.DisplayName = new DTLocalizable { OutputString = true };
				foreach (var kvp in src.DisplayName)
					dst.DisplayName.Add(kvp.Key, kvp.Value);
			}

			return dst;
		}
	}
}
