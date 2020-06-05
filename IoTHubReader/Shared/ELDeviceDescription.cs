using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace IoTHubReader.Shared
{
	public enum ELAccessRule
	{
		None,
		Required,
		ByCase,
		Optional,
		NA,
	}

	public enum ELDataType
	{
		None,
		State,
		Object,
		DateTime,
		Time,
		Raw,
		Array,
		Bitmap,
		Level,
		Number,
		NumericValue,
		OneOf,
	}

	public enum ELNumberFormat
	{
		None,
		Int8,
		Int16,
		Int32,
		Uint8,
		Uint16,
		Uint32,
	}

	public class EdtInfo
	{
		public long Edt;
		public string StateJa;
		public string StateEn;
		public double NumericValue;
		public bool EdtReadOnly;
	}

	public class ELDeviceDescription
	{
		enum State
		{
			None,
			Root,
			MetaData,
			Definitions,
			Devices,
		}

		public ELMetaData MetaData { get; private set; }
		public List<ELDefinition> Definitions { get; private set; }
		public List<ELDevice> Devices { get; private set; }

		public void WalkJson(ref Utf8JsonReader reader)
		{
			State state = State.None;

			while (reader.Read()) {
				switch (reader.TokenType) {
				case JsonTokenType.StartObject:
					switch (state) {
					case State.None:
						state = State.Root;
						break;
					case State.MetaData:
						MetaData = new ELMetaData();
						MetaData.WalkJson(ref reader);
						state = State.Root;
						break;
					case State.Definitions:
						Definitions = new List<ELDefinition>();
						break;
					case State.Devices:
						Devices = new List<ELDevice>();
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.EndObject:
					switch (state) {
					case State.Root:
						return;
					case State.Definitions:
					case State.Devices:
						state = State.Root;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.PropertyName: {
					var propertyName = reader.GetString();
					switch (state) {
					case State.Root:
						switch (propertyName) {
						case "metaData":
							state = State.MetaData;
							break;
						case "definitions":
							state = State.Definitions;
							break;
						case "devices":
							state = State.Devices;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.Definitions: {
						for (; ; ) {
							var definition = new ELDefinition(propertyName);
							definition.WalkJson(ref reader, true);
							Definitions.Add(definition);
							reader.Read();
							if (reader.TokenType != JsonTokenType.PropertyName)
								break;
							propertyName = reader.GetString();
						}
						state = State.Root;
						break;
					}
					case State.Devices:
						for (; ; ) {
							var device = new ELDevice(propertyName);
							device.WalkJson(ref reader, true);
							Devices.Add(device);
							reader.Read();
							if (reader.TokenType != JsonTokenType.PropertyName)
								break;
							propertyName = reader.GetString();
						}
						state = State.Root;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				default:
					System.Diagnostics.Debugger.Break();
					break;
				}
			}
		}

		public ELDefinition GetDefinition(string reference)
		{
			string name;
			if (reference.StartsWith("#/definitions/"))
				name = reference.Substring(14);
			else
				return null;

			foreach (var def in Definitions) {
				if (def.Name == name)
					return def;
			}

			return null;
		}
	}

	public class ELMetaData
	{
		enum State
		{
			None,
			Members,
			Date,
			Release,
			Version,
			Copyright,
		}

		public string Date { get; private set; }
		public string Release { get; private set; }
		public string Version { get; private set; }
		public string Copyright { get; private set; }

		public void WalkJson(ref Utf8JsonReader reader)
		{
			State state = State.Members;

			while (reader.Read()) {
				switch (reader.TokenType) {
				case JsonTokenType.EndObject:
					switch (state) {
					case State.Members:
						return;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.PropertyName: {
					var propertyName = reader.GetString();
					switch (propertyName) {
					case "date":
						state = State.Date;
						break;
					case "release":
						state = State.Release;
						break;
					case "version":
						state = State.Version;
						break;
					case "Copyright":
						state = State.Copyright;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				case JsonTokenType.String: {
					var value = reader.GetString();
					switch (state) {
					case State.Date:
						Date = value;
						state = State.Members;
						break;
					case State.Release:
						Release = value;
						state = State.Members;
						break;
					case State.Version:
						Version = value;
						state = State.Members;
						break;
					case State.Copyright:
						Copyright = value;
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				default:
					System.Diagnostics.Debugger.Break();
					break;
				}
			}
		}
	}

	public class ELDefinition
	{
		enum State
		{
			None,
			Members,
			Name,
			Type,
			Size,
			Format,
			Minimum,
			Maximum,
			MinSize,
			MaxSize,
			Unit,
			Reference,
			MultipleOf,
			ItemSize,
			MinItems,
			MaxItems,
			Base,
			NumberEnum,
			Enum,
			Coefficient,
			CoefficientEpc,
			Properties,
			Property,
			Edt,
			EdtEdt,
			EdtState,
			EdtNumericValue,
			EdtReadOnly,
			EdtStateJa,
			EdtStateEn,
			Element,
			OneOf,
			OneOfItem,
			Items,
			Bitmaps,
			Bitmap
		}

		public EdtInfo EdtInfo { get; private set; }
		public ELDataType Type { get; private set; }
		public int Size { get; private set; }
		public string Name { get; private set; }
		public ELNumberFormat NumFormat { get; private set; }
		public int Minimum { get; private set; }
		public int Maximum { get; private set; }
		public int MinSize { get; private set; }
		public int MaxSize { get; private set; }
		public string Unit { get; private set; }
		public string Reference { get; private set; }
		public double MultipleOf { get; private set; }
		public int ItemSize { get; private set; }
		public int MinItems { get; private set; }
		public int MaxItems { get; private set; }
		public string BaseName { get; private set; }
		public int NumberEnum { get; private set; }
		public List<EdtInfo> EdtInfos { get; private set; }
		public List<string> CoefficientEpcs { get; private set; }
		public List<ELDefinition> DataInfos { get; private set; }
		public List<ELBitmap> BitmapInfos { get; private set; }

		public ELDefinition()
		{
		}

		public ELDefinition(string name)
		{
			this.Name = name;
		}

		public void WalkJson(ref Utf8JsonReader reader, bool start)
		{
			State state = start ? State.None : State.Members;

			while (reader.Read()) {
				switch (reader.TokenType) {
				case JsonTokenType.None:
					Console.WriteLine("read error.");
					return;
				case JsonTokenType.StartObject:
					switch (state) {
					case State.None:
						state = State.Members;
						break;
					case State.Members:
						System.Diagnostics.Debugger.Break();
						break;
					case State.Edt:
						EdtInfo = new EdtInfo();
						break;
					case State.EdtState:
						break;
					case State.Property: {
						do {
							var definition = new ELDefinition();
							definition.WalkJson(ref reader, false);
							DataInfos.Add(definition);
							reader.Read();
						} while (reader.TokenType == JsonTokenType.StartObject);
						state = State.Members;
						break;
					}
					case State.Element: {
						WalkJson(ref reader, false);
						state = State.Members;
						break;
					}
					case State.OneOfItem:
						do {
							var definition = new ELDefinition();
							definition.WalkJson(ref reader, false);
							DataInfos.Add(definition);
							reader.Read();
						} while (reader.TokenType == JsonTokenType.StartObject);
						Type = ELDataType.OneOf;
						state = State.Members;
						break;
					case State.Items: {
						DataInfos = new List<ELDefinition>();
						var definition = new ELDefinition();
						definition.WalkJson(ref reader, false);
						DataInfos.Add(definition);
						state = State.Members;
						break;
					}
					case State.Bitmap:
						do {
							var bitmap = new ELBitmap();
							bitmap.WalkJson(ref reader);
							BitmapInfos.Add(bitmap);
							reader.Read();
						} while (reader.TokenType == JsonTokenType.StartObject);
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.EndObject:
					switch (state) {
					case State.Members:
						return;
					case State.Edt:
						state = State.Edt;
						EdtInfos.Add(EdtInfo);
						EdtInfo = null;
						break;
					case State.EdtState:
						state = State.Edt;
						break;
					case State.OneOf:
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.StartArray:
					switch (state) {
					case State.Enum:
						EdtInfos = new List<EdtInfo>();
						state = State.Edt;
						break;
					case State.Properties:
						DataInfos = new List<ELDefinition>();
						state = State.Property;
						break;
					case State.Coefficient:
						CoefficientEpcs = new List<string>();
						state = State.CoefficientEpc;
						break;
					case State.OneOf:
						DataInfos = new List<ELDefinition>();
						state = State.OneOfItem;
						break;
					case State.Bitmaps:
						BitmapInfos = new List<ELBitmap>();
						state = State.Bitmap;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.EndArray:
					switch (state) {
					case State.Edt:
						state = State.Members;
						break;
					case State.Property:
						state = State.Properties;
						break;
					case State.CoefficientEpc:
						state = State.Members;
						break;
					case State.OneOfItem:
						state = State.OneOf;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.PropertyName: {
					var propertyName = reader.GetString();
					switch (state) {
					case State.Members:
						switch (propertyName) {
						case "name":
							state = State.Name;
							break;
						case "type":
							state = State.Type;
							break;
						case "size":
							state = State.Size;
							break;
						case "enum":
							state = State.Enum;
							break;
						case "properties":
							state = State.Properties;
							break;
						case "unit":
							state = State.Unit;
							break;
						case "multipleOf":
							state = State.MultipleOf;
							break;
						case "minSize":
							state = State.MinSize;
							break;
						case "maxSize":
							state = State.MaxSize;
							break;
						case "itemSize":
							state = State.ItemSize;
							break;
						case "minItems":
							state = State.MinItems;
							break;
						case "maxItems":
							state = State.MaxItems;
							break;
						case "base":
							state = State.Base;
							break;
						case "minimum":
							state = State.Minimum;
							break;
						case "maximum":
							state = State.Maximum;
							break;
						case "coefficient":
							state = State.Coefficient;
							break;
						case "items":
							state = State.Items;
							break;
						case "bitmaps":
							state = State.Bitmaps;
							break;
						case "oneOf":
							state = State.OneOf;
							break;
						case "format":
							state = State.Format;
							break;
						case "$ref":
							state = State.Reference;
							break;
						case "element":
							state = State.Element;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.Edt:
						switch (propertyName) {
						case "edt":
							state = State.EdtEdt;
							break;
						case "state":
							state = State.EdtState;
							break;
						case "numericValue":
							state = State.EdtNumericValue;
							break;
						case "readOnly":
							state = State.EdtReadOnly;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.EdtState:
						switch (propertyName) {
						case "ja":
							state = State.EdtStateJa;
							break;
						case "en":
							state = State.EdtStateEn;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				case JsonTokenType.Comment:
					break;
				case JsonTokenType.String: {
					var value = reader.GetString();
					switch (state) {
					case State.Type:
						Type = GetDefinitionType(value);
						state = State.Members;
						break;
					case State.Name:
						Name = value;
						state = State.Members;
						break;
					case State.Size:
						Size = Int32.Parse(value);
						state = State.Members;
						break;
					case State.Format:
						NumFormat = GetDefinitionFormat(value);
						state = State.Members;
						break;
					case State.Minimum:
						Minimum = GetIntger(NumFormat, value);
						state = State.Members;
						break;
					case State.Maximum:
						Maximum = GetIntger(NumFormat, value);
						state = State.Members;
						break;
					case State.MinSize:
						MinSize = Int32.Parse(value);
						state = State.Members;
						break;
					case State.MaxSize:
						MaxSize = Int32.Parse(value);
						state = State.Members;
						break;
					case State.Unit:
						Unit = value;
						state = State.Members;
						break;
					case State.Reference:
						Reference = value;
						state = State.Members;
						break;
					case State.MultipleOf:
						MultipleOf = Double.Parse(value);
						state = State.Members;
						break;
					case State.ItemSize:
						ItemSize = Int32.Parse(value);
						state = State.Members;
						break;
					case State.MinItems:
						MinItems = Int32.Parse(value);
						state = State.Members;
						break;
					case State.MaxItems:
						MaxItems = Int32.Parse(value);
						state = State.Members;
						break;
					case State.Base:
						BaseName = value;
						state = State.Members;
						break;
					case State.NumberEnum:
						NumberEnum = Int32.Parse(value);
						state = State.Members;
						break;
					case State.EdtEdt:
						EdtInfo.Edt = GetEdt(value);
						state = State.Edt;
						break;
					case State.EdtNumericValue:
						EdtInfo.NumericValue = Double.Parse(value);
						state = State.Edt;
						break;
					case State.EdtReadOnly:
						EdtInfo.EdtReadOnly = Boolean.Parse(value);
						state = State.Edt;
						break;
					case State.EdtStateJa:
						EdtInfo.StateJa = value;
						state = State.EdtState;
						break;
					case State.EdtStateEn:
						EdtInfo.StateEn = value;
						state = State.EdtState;
						break;
					case State.CoefficientEpc:
						CoefficientEpcs.Add(value);
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				case JsonTokenType.Number:
					switch (state) {
					case State.Size:
						Size = reader.GetInt32();
						state = State.Members;
						break;
					case State.Minimum:
						Minimum = GetIntger(NumFormat, reader);
						state = State.Members;
						break;
					case State.Maximum:
						Maximum = GetIntger(NumFormat, reader);
						state = State.Members;
						break;
					case State.MinSize:
						MinSize = reader.GetInt32();
						state = State.Members;
						break;
					case State.MaxSize:
						MaxSize = reader.GetInt32();
						state = State.Members;
						break;
					case State.MultipleOf:
						MultipleOf = reader.GetDouble();
						state = State.Members;
						break;
					case State.ItemSize:
						ItemSize = reader.GetInt32();
						state = State.Members;
						break;
					case State.MinItems:
						MinItems = reader.GetInt32();
						state = State.Members;
						break;
					case State.MaxItems:
						MaxItems = reader.GetInt32();
						state = State.Members;
						break;
					case State.NumberEnum:
						NumberEnum = reader.GetInt32();
						state = State.Members;
						break;
					case State.Edt: {
						var edt = new EdtInfo { Edt = reader.GetInt32() };
						EdtInfos.Add(edt);
						state = State.Edt;
						break;
					}
					case State.EdtEdt:
						EdtInfo.Edt = reader.GetInt32();
						state = State.Edt;
						break;
					case State.EdtNumericValue:
						EdtInfo.NumericValue = reader.GetDouble();
						state = State.Edt;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.True:
					switch (state) {
					case State.EdtReadOnly:
						EdtInfo.EdtReadOnly = reader.GetBoolean();
						state = State.Edt;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.False:
					switch (state) {
					case State.EdtReadOnly:
						EdtInfo.EdtReadOnly = reader.GetBoolean();
						state = State.Edt;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.Null:
					System.Diagnostics.Debugger.Break();
					break;
				default:
					System.Diagnostics.Debugger.Break();
					break;
				}

				// Other token types elided for brevity
			}
		}

		private static long GetEdt(string value)
		{
			if (value.StartsWith("0x")) {
				return Int64.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber);
			}
			return Int64.Parse(value);
		}

		private static int GetIntger(ELNumberFormat format, Utf8JsonReader reader)
		{
			switch (format) {
			case ELNumberFormat.None:
				break;
			case ELNumberFormat.Int8:
				return reader.GetSByte();
			case ELNumberFormat.Int16:
				return reader.GetInt16();
			case ELNumberFormat.Int32:
				return reader.GetInt32();
			case ELNumberFormat.Uint8:
				return reader.GetByte();
			case ELNumberFormat.Uint16:
				return reader.GetUInt16();
			case ELNumberFormat.Uint32:
				return (int)reader.GetUInt32();
			}
			return reader.GetInt32();
		}

		private static int GetIntger(ELNumberFormat format, string value)
		{
			switch (format) {
			case ELNumberFormat.None:
				break;
			case ELNumberFormat.Int8:
				return SByte.Parse(value);
			case ELNumberFormat.Int16:
				return Int16.Parse(value);
			case ELNumberFormat.Int32:
				return Int32.Parse(value);
			case ELNumberFormat.Uint8:
				return Byte.Parse(value);
			case ELNumberFormat.Uint16:
				return UInt16.Parse(value);
			case ELNumberFormat.Uint32:
				return (int)UInt32.Parse(value);
			}
			System.Diagnostics.Debugger.Break();
			return 0;
		}

		private static ELNumberFormat GetDefinitionFormat(string value)
		{
			switch (value) {
			case "int8":
				return ELNumberFormat.Int8;
			case "int16":
				return ELNumberFormat.Int16;
			case "int32":
				return ELNumberFormat.Int32;
			case "uint8":
				return ELNumberFormat.Uint8;
			case "uint16":
				return ELNumberFormat.Uint16;
			case "uint32":
				return ELNumberFormat.Uint32;
			}
			System.Diagnostics.Debugger.Break();
			return ELNumberFormat.None;
		}

		private static ELDataType GetDefinitionType(string value)
		{
			switch (value) {
			case "state":
				return ELDataType.State;
			case "object":
				return ELDataType.Object;
			case "date-time":
				return ELDataType.DateTime;
			case "time":
				return ELDataType.Time;
			case "raw":
				return ELDataType.Raw;
			case "array":
				return ELDataType.Array;
			case "bitmap":
				return ELDataType.Bitmap;
			case "level":
				return ELDataType.Level;
			case "number":
				return ELDataType.Number;
			case "numericValue":
				return ELDataType.NumericValue;
			case "oneOf":
				return ELDataType.OneOf;
			}
			System.Diagnostics.Debugger.Break();
			return ELDataType.None;
		}
	}

	public class ELDevice
	{
		enum State
		{
			None,
			Members,
			ValidRelease,
			ValidReleaseValue,
			ValidReleaseFrom,
			ValidReleaseTo,
			ClassName,
			ClassNameValue,
			ClassNameJa,
			ClassNameEn,
			ElProperties,
			ElPropertieValue,
			OneOf,
			OneOfItem,
			FirstRelease,
		}

		public string Code { get; private set; }
		public string From { get; private set; }
		public string To { get; private set; }
		public string FirstRelease { get; private set; }
		public string Ja { get; private set; }
		public string En { get; private set; }
		public List<ELProperty> Properties { get; private set; }
		public List<ELDevice> OneOf { get; private set; }

		public ELDevice(string code)
		{
			this.Code = code;
		}

		public void WalkJson(ref Utf8JsonReader reader, bool start)
		{
			State state = start ? State.None : State.Members;

			while (reader.Read()) {
				switch (reader.TokenType) {
				case JsonTokenType.None:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.StartObject:
					switch (state) {
					case State.None:
						state = State.Members;
						break;
					case State.ValidRelease:
						state = State.ValidReleaseValue;
						break;
					case State.ClassName:
						state = State.ClassNameValue;
						break;
					case State.ElProperties:
						Properties = new List<ELProperty>();
						state = State.ElPropertieValue;
						break;
					case State.OneOfItem:
						do {
							var device = new ELDevice(Code);
							device.WalkJson(ref reader, false);
							OneOf.Add(device);
							reader.Read();
						} while (reader.TokenType == JsonTokenType.StartObject);
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.EndObject:
					switch (state) {
					case State.Members:
						return;
					case State.ValidReleaseValue:
						state = State.Members;
						break;
					case State.ClassNameValue:
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.StartArray:
					switch (state) {
					case State.OneOf:
						OneOf = new List<ELDevice>();
						state = State.OneOfItem;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.EndArray:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.PropertyName: {
					var propertyName = reader.GetString();
					switch (state) {
					case State.Members:
						switch (propertyName) {
						case "validRelease":
							state = State.ValidRelease;
							break;
						case "className":
							state = State.ClassName;
							break;
						case "firstRelease":
							state = State.FirstRelease;
							break;
						case "elProperties":
							state = State.ElProperties;
							break;
						case "oneOf":
							state = State.OneOf;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.ValidReleaseValue:
						switch (propertyName) {
						case "from":
							state = State.ValidReleaseFrom;
							break;
						case "to":
							state = State.ValidReleaseTo;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.ClassNameValue:
						switch (propertyName) {
						case "ja":
							state = State.ClassNameJa;
							break;
						case "en":
							state = State.ClassNameEn;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.ElPropertieValue:
						for (; ; ) {
							var property = new ELProperty(propertyName);
							property.WalkJson(ref reader, true);
							Properties.Add(property);
							reader.Read();
							if (reader.TokenType != JsonTokenType.PropertyName)
								break;
							propertyName = reader.GetString();
						}
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				case JsonTokenType.Comment:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.String: {
					var value = reader.GetString();
					switch (state) {
					case State.ValidReleaseFrom:
						From = value;
						state = State.ValidReleaseValue;
						break;
					case State.ValidReleaseTo:
						To = value;
						state = State.ValidReleaseValue;
						break;
					case State.ClassNameJa:
						Ja = value;
						state = State.ClassNameValue;
						break;
					case State.ClassNameEn:
						En = value;
						state = State.ClassNameValue;
						break;
					case State.FirstRelease:
						FirstRelease = value;
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				case JsonTokenType.Number:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.True:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.False:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.Null:
					System.Diagnostics.Debugger.Break();
					break;
				}
			}
		}
	}

	public class ELProperty
	{
		enum State
		{
			None,
			Members,
			ValidRelease,
			ValidReleaseValue,
			ValidReleaseFrom,
			ValidReleaseTo,
			PropertyName,
			PropertyNameValue,
			PropertyNameJa,
			PropertyNameEn,
			AccessRule,
			Data,
			AccessRuleValue,
			AccessRuleGet,
			AccessRuleSet,
			AccessRuleInf,
			Note,
			NoteValue,
			NoteJa,
			NoteEn,
			OneOf,
			OneOfItem,
			Atomic,
		}

		public int Epc { get; private set; }
		public string From { get; private set; }
		public string To { get; private set; }
		public string Ja { get; private set; }
		public string En { get; private set; }
		public ELAccessRule Get { get; private set; }
		public ELAccessRule Set { get; private set; }
		public ELAccessRule Inf { get; private set; }
		public ELDefinition Data { get; private set; }
		public string NoteJa { get; private set; }
		public string NoteEn { get; private set; }
		public int Atmic { get; private set; }
		public List<ELProperty> OneOf { get; private set; }

		public ELProperty(string epc)
		{
			this.Epc = GetEpc(epc);
		}

		private static byte GetEpc(string epc)
		{
			if (epc.StartsWith("0x")) {
				return Byte.Parse(epc.Substring(2), System.Globalization.NumberStyles.HexNumber);
			}
			else {
				return Byte.Parse(epc);
			}
		}

		public void WalkJson(ref Utf8JsonReader reader, bool start)
		{
			State state = start ? State.None : State.Members;

			while (reader.Read()) {
				switch (reader.TokenType) {
				case JsonTokenType.None:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.StartObject:
					switch (state) {
					case State.None:
						state = State.Members;
						break;
					case State.ValidRelease:
						state = State.ValidReleaseValue;
						break;
					case State.PropertyName:
						state = State.PropertyNameValue;
						break;
					case State.AccessRule:
						state = State.AccessRuleValue;
						break;
					case State.Data: {
						Data = new ELDefinition();
						Data.WalkJson(ref reader, false);
						state = State.Members;
						break;
					}
					case State.Note:
						state = State.NoteValue;
						break;
					case State.OneOfItem:
						do {
							var property = new ELProperty(Epc.ToString());
							property.WalkJson(ref reader, false);
							OneOf.Add(property);
							reader.Read();
						} while (reader.TokenType == JsonTokenType.StartObject);
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.EndObject:
					switch (state) {
					case State.Members:
						return;
					case State.ValidReleaseValue:
						state = State.Members;
						break;
					case State.PropertyNameValue:
						state = State.Members;
						break;
					case State.AccessRuleValue:
						state = State.Members;
						break;
					case State.NoteValue:
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.StartArray:
					switch (state) {
					case State.OneOf:
						OneOf = new List<ELProperty>();
						state = State.OneOfItem;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.EndArray:
					switch (state) {
					case State.OneOfItem:
						state = State.None;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.PropertyName: {
					var propertyName = reader.GetString();
					switch (state) {
					case State.Members:
						switch (propertyName) {
						case "validRelease":
							state = State.ValidRelease;
							break;
						case "propertyName":
							state = State.PropertyName;
							break;
						case "accessRule":
							state = State.AccessRule;
							break;
						case "data":
							state = State.Data;
							break;
						case "note":
							state = State.Note;
							break;
						case "oneOf":
							state = State.OneOf;
							break;
						case "atomic":
							state = State.Atomic;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.ValidReleaseValue:
						switch (propertyName) {
						case "from":
							state = State.ValidReleaseFrom;
							break;
						case "to":
							state = State.ValidReleaseTo;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.PropertyNameValue:
						switch (propertyName) {
						case "ja":
							state = State.PropertyNameJa;
							break;
						case "en":
							state = State.PropertyNameEn;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.AccessRuleValue:
						switch (propertyName) {
						case "get":
							state = State.AccessRuleGet;
							break;
						case "set":
							state = State.AccessRuleSet;
							break;
						case "inf":
							state = State.AccessRuleInf;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.NoteValue:
						switch (propertyName) {
						case "ja":
							state = State.NoteJa;
							break;
						case "en":
							state = State.NoteEn;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				case JsonTokenType.Comment:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.String: {
					var value = reader.GetString();
					switch (state) {
					case State.ValidReleaseFrom:
						From = value;
						state = State.ValidReleaseValue;
						break;
					case State.ValidReleaseTo:
						To = value;
						state = State.ValidReleaseValue;
						break;
					case State.PropertyNameJa:
						Ja = value;
						state = State.PropertyNameValue;
						break;
					case State.PropertyNameEn:
						En = value;
						state = State.PropertyNameValue;
						break;
					case State.AccessRuleGet:
						Get = ParseAccessRule(value);
						state = State.AccessRuleValue;
						break;
					case State.AccessRuleSet:
						Set = ParseAccessRule(value);
						state = State.AccessRuleValue;
						break;
					case State.AccessRuleInf:
						Inf = ParseAccessRule(value);
						state = State.AccessRuleValue;
						break;
					case State.NoteJa:
						NoteJa = value;
						state = State.NoteValue;
						break;
					case State.NoteEn:
						NoteEn = value;
						state = State.NoteValue;
						break;
					case State.Atomic:
						Atmic = GetEpc(value);
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				case JsonTokenType.Number:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.True:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.False:
					System.Diagnostics.Debugger.Break();
					break;
				case JsonTokenType.Null:
					System.Diagnostics.Debugger.Break();
					break;
				}
			}
		}

		public static ELAccessRule ParseAccessRule(string value)
		{
			switch (value) {
			case "required":
				return ELAccessRule.Required;
			case "required_c":
				return ELAccessRule.ByCase;
			case "optional":
				return ELAccessRule.Optional;
			case "notApplicable":
				return ELAccessRule.NA;
			}
			return ELAccessRule.None;
		}
	}

	public class ELBitmap
	{
		enum State
		{
			None,
			Members,
			Name,
			Descriptions,
			Position,
			Value,
			DescriptionsValue,
			DescriptionsJa,
			DescriptionsEn,
			PositionValue,
			PositionIndex,
			PositionBitMask,
		}

		public string Name { get; private set; }
		public string Ja { get; private set; }
		public string En { get; private set; }
		public int Index { get; private set; }
		public string BitMask { get; private set; }
		public ELDefinition Value { get; private set; }

		public void WalkJson(ref Utf8JsonReader reader)
		{
			State state = State.Members;

			while (reader.Read()) {
				switch (reader.TokenType) {
				case JsonTokenType.None:
					break;
				case JsonTokenType.StartObject:
					switch (state) {
					case State.None:
						state = State.Members;
						break;
					case State.Descriptions:
						state = State.DescriptionsValue;
						break;
					case State.Position:
						state = State.PositionValue;
						break;
					case State.Value:
						Value = new ELDefinition();
						Value.WalkJson(ref reader, false);
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.EndObject:
					switch (state) {
					case State.Members:
						return;
					case State.DescriptionsValue:
						state = State.Members;
						break;
					case State.PositionValue:
						state = State.Members;
						break;
					case State.Value:
						state = State.Members;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.StartArray:
					break;
				case JsonTokenType.EndArray:
					break;
				case JsonTokenType.PropertyName: {
					var propertyName = reader.GetString();
					switch (state) {
					case State.Members:
						switch (propertyName) {
						case "name":
							state = State.Name;
							break;
						case "descriptions":
							state = State.Descriptions;
							break;
						case "position":
							state = State.Position;
							break;
						case "value":
							state = State.Value;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.DescriptionsValue:
						switch (propertyName) {
						case "ja":
							state = State.DescriptionsJa;
							break;
						case "en":
							state = State.DescriptionsEn;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					case State.PositionValue:
						switch (propertyName) {
						case "index":
							state = State.PositionIndex;
							break;
						case "bitMask":
							state = State.PositionBitMask;
							break;
						default:
							System.Diagnostics.Debugger.Break();
							break;
						}
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				case JsonTokenType.Comment:
					break;
				case JsonTokenType.String: {
					var value = reader.GetString();
					switch (state) {
					case State.Name:
						Name = value;
						state = State.Members;
						break;
					case State.DescriptionsJa:
						Ja = value;
						state = State.DescriptionsValue;
						break;
					case State.DescriptionsEn:
						En = value;
						state = State.DescriptionsValue;
						break;
					case State.PositionBitMask:
						BitMask = value;
						state = State.PositionValue;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				}
				case JsonTokenType.Number:
					switch (state) {
					case State.PositionIndex:
						Index = reader.GetInt32();
						state = State.PositionValue;
						break;
					default:
						System.Diagnostics.Debugger.Break();
						break;
					}
					break;
				case JsonTokenType.True:
					break;
				case JsonTokenType.False:
					break;
				case JsonTokenType.Null:
					break;
				default:
					System.Diagnostics.Debugger.Break();
					break;
				}
			}
		}
	}
}
