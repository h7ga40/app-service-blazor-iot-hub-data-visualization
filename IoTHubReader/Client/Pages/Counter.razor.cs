using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using IoTCentral;
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

			root = new ELDeviceDescription();
			root.WalkJson(ref reader);

			var dtifs = new List<DTInterface>();

			foreach (var device in root.Devices) {
				WalkProperties(dtifs, device);

				if (device.OneOf != null) {
					foreach (var one in device.OneOf) {
						WalkProperties(dtifs, one);
					}
				}
			}
		}

		private void WalkProperties(List<DTInterface> dtifs, ELDevice device)
		{
			if (device.Properties != null) {
				foreach (var property in device.Properties) {
					MakeDTInterface(dtifs, 0, GetAccessValue(property),
						property.Ja, property.En, property.Data);

					if (property.OneOf != null) {
						foreach (var one in property.OneOf) {
							MakeDTInterface(dtifs, 0, GetAccessValue(one),
									one.Ja, one.En, one.Data);
						}
					}
				}
			}
		}

		ELDeviceDescription root;

		private uint GetAccessValue(ELProperty property)
		{
			return GetAccessValue(property.Get, property.Set, property.Inf);
		}

		internal uint GetAccessValue(ELAccessRule get, ELAccessRule set, ELAccessRule inf)
		{
			return (((uint)inf & 0xF) << 8) | (((uint)set & 0xF) << 4) | (((uint)get & 0xF) << 0);
		}

		internal string make_digital_twin_id(string id)
		{
			int i = 0, len = id.Length;
			char[] temp = new char[len + 1];

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

			if (i >= len - 1)
				i = len - 1;

			temp[i] = '\0';

			return new string(temp);
		}


		private void MakeDTInterface(List<DTInterface> dtifs, int index, uint accessValue,
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
					DTInterface command = new DTInterface();

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
					command.DisplayNameEn = edt.StateEn;
					command.DisplayNameJa = edt.StateJa;

					command.CommandType = "synchronous";

					dtifs.Add(command);
				}
			}
			else {
				DTInterface ifcnt = new DTInterface();

				var name = make_digital_twin_id(propertyNameEn);

				string temp;
				if (index == 0) {
					temp = $"urn:EchonetLite:{name}:1";
				}
				else {
					temp = $"urn:EchonetLite:{name}{index + 1}:1";
				}

				ifcnt.Id = temp;

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

				ifcnt.DisplayNameEn = propertyNameEn;
				ifcnt.DisplayNameJa = propertyNameJa;

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

		DTSchema make_schema(ELDefinition dataInfo)
		{
			var schema = new DTSchema();

			switch (dataInfo.Type) {
			case ELDataType.None:
				if (!String.IsNullOrEmpty(dataInfo.Reference)) {
					return make_schema(root.GetDefinition(dataInfo.Reference));
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

					enumValue.DisplayNameEn = edt.StateEn;
					enumValue.DisplayNameJa = edt.StateJa;
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

		DTCommand make_command_payload(string propertyNameJa, string propertyNameEn, ELDefinition dataInfo)
		{
			var command = new DTCommand();

			var name = make_digital_twin_id(propertyNameEn);

			var temp = $"urn:EchonetLite:{name}:1";

			command.Id = temp;

			command.Name = name;

			var request = make_schema(dataInfo);
			command.Schema = request;

			command.DisplayNameEn = propertyNameEn;
			command.DisplayNameJa = propertyNameJa;

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

	class DTInterface
	{
		public string Id { get; internal set; }
		public string Type { get; internal set; }
		public string Context { get; internal set; }
		public string Name { get; internal set; }
		public string DisplayNameEn { get; internal set; }
		public string DisplayNameJa { get; internal set; }
		public string CommandType { get; internal set; }
		public DTSchema Schema { get; internal set; }
		public bool Writable { get; internal set; }
		public DTCommand Request { get; internal set; }
		public DTCommand Response { get; internal set; }
		public string DisplayUnit { get; internal set; }
	}

	class DTSchema
	{
		public string Type { get; internal set; }
		public List<DTField> Fields { get; internal set; }
		public string ValueSchema { get; internal set; }
		public List<DTEnumValue> EnumValues { get; internal set; }
	}

	class DTCommand
	{
		public string Id { get; internal set; }
		public string Name { get; internal set; }
		public DTSchema Schema { get; internal set; }
		public string DisplayNameEn { get; internal set; }
		public string DisplayNameJa { get; internal set; }
		public string DisplayUnit { get; internal set; }
	}

	class DTField
	{
		public string Name { get; internal set; }
		public DTSchema Schema { get; internal set; }
	}

	class DTEnumValue
	{
		public string Name { get; internal set; }
		public long EnumValue { get; internal set; }
		public string DisplayNameEn { get; internal set; }
		public string DisplayNameJa { get; internal set; }
	}
}
