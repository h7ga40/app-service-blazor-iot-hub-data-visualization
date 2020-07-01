using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IoTHubReader.Shared;
using IoTHubReader.Client.Components;

namespace IoTHubReader.Client.Pages
{
	partial class DeviceTwin
	{
		private string GenerateRuby(DTCapabilityModel deviceTemplate, string moduleName)
		{
			string rubyCode = "";

			foreach (var implement in deviceTemplate.Implements) {
				using (var writer = new CodeWriter()) {
					OutputRuby(implement.Schema, moduleName, implement.Name, writer);
					rubyCode += writer.ToString();
				}
			}

			return rubyCode;
		}

		private static void OutputRuby(DTSchema ifInstance, string moduleName, string className, CodeWriter stream)
		{
			var cls = new RBClass { Name = className };
			foreach (var content in ifInstance.Contents) {
				if (content.Type.Contains("Telemetry")) {
					if (content.Type.Contains("SemanticType/State")) {
						cls.States.Add(content);
					}
					else {
						cls.Telemetries.Add(content);
					}
				}
				else if (content.Type.Contains("Command")) {
					cls.Commands.Add(content);
				}
				else if (content.Type.Contains("Property")) {
					cls.Properties.Add(content);
				}
			}

			stream.WriteLine("module " + moduleName);
			stream.Indent++;
			OutputRuby(cls, stream);
			stream.Indent--;
			stream.WriteLine("end");

			stream.WriteLine();

			OutputMainRoop(moduleName, className, stream);
		}

		private static void OutputMainRoop(string moduleName, string className, CodeWriter stream)
		{
			stream.WriteLine($"twin = {moduleName}::{className}.new");
			stream.WriteLine();
			stream.WriteLine(@"connectionString = AzureIoT.get_connection_string
protocol = AzureIoT::MQTT

client = AzureIoT::DeviceClient.new(connectionString, protocol)
client.set_twin(twin)

while true do
  twin.measure
  message = twin.get_message

  done = false
  client.send_event(message) do
    puts ""sent message""
    done = true
  end

  count = 5000
  while !done do
    client.do_work
    sleep(0.001)
    if count > 0 then
      count -= 1
    end
  end

  if count > 0 then
    sleep(0.001 * count)
  end
end");
		}

		private static void OutputRuby(RBClass cls, CodeWriter stream)
		{
			stream.WriteLine("class " + cls.Name);
			stream.Indent++;

			// attr_reader
			foreach (var t in cls.Telemetries) {
				stream.WriteLine("attr_reader :" + t.Name);
			}
			if (cls.Telemetries.Count > 0)
				stream.WriteLine();

			stream.WriteLine("def initialize");
			stream.Indent++;
			foreach (var p in cls.Properties) {
				stream.WriteLine("@" + p.Name + " = rand()");
			}
			stream.Indent--;
			stream.WriteLine("end");
			stream.WriteLine();

			foreach (var c in cls.Commands) {
				stream.WriteLine("def " + c.Name + "(peyload)");
				stream.Indent++;
				stream.WriteLine("puts \"execute " + c.Name + " \" + peyload");
				stream.WriteLine("\"{\\\"Message\\\":\\\"execute " + c.Name + " with Method\\\"}\"");
				stream.Indent--;
				stream.WriteLine("end");
				stream.WriteLine();
			}

			foreach (var s in cls.States) {
				stream.WriteLine("def get_" + s.Name.ToLowerCaseUnderbar());
				stream.Indent++;
				stream.WriteLine("@" + s.Name);
				stream.Indent--;
				stream.WriteLine("end");
				stream.WriteLine();
			}

			foreach (var p in cls.Properties) {
				stream.WriteLine("def set_" + p.Name.ToLowerCaseUnderbar() + "(value)");
				stream.Indent++;
				stream.WriteLine("@" + p.Name + " = value");
				stream.WriteLine("puts \"set " + p.Name + " \" + value");
				stream.Indent--;
				stream.WriteLine("end");
				stream.WriteLine();
			}

			// def recv_twin
			OutputRecvTwinMethod(cls, stream);
			stream.WriteLine();

			// def get_message
			OutputGetMessageMethod(cls, stream);
			stream.WriteLine();

			// def measure
			OutputMeasureMethod(cls, stream);

			stream.Indent--;
			stream.WriteLine("end");
		}

		private static void OutputRecvTwinMethod(RBClass cls, CodeWriter stream)
		{
			stream.WriteLine(@"def recv_twin(peyload)
  json = JSON.parse(peyload)
  desired = json[""desired""]
  if desired == nil
    desired = json
  end
  desired.each{|key, obj|");
			stream.Indent += 2;

			if (cls.Properties.Count > 0) {
				stream.WriteLine("case key");
				foreach (var p in cls.Properties) {
					stream.WriteLine("when \"" + p.Name + "\"");
					stream.Indent++;
					stream.WriteLine("value = obj[\"value\"]");
					stream.WriteLine("if value != nil");
					stream.Indent++;
					stream.WriteLine("set_" + p.Name.ToLowerCaseUnderbar() + "(value)");
					stream.WriteLine("desired[key] = {value: @" + p.Name + ", status: \"success\"}");
					stream.Indent--;
					stream.WriteLine("else");
					stream.Indent++;
					stream.WriteLine("desired[key] = nil");
					stream.Indent--;
					stream.WriteLine("end");
					stream.Indent--;
				}
				stream.WriteLine("else");
				stream.Indent++;
				stream.WriteLine("desired[key] = nil");
				stream.Indent--;
				stream.WriteLine("end");
			}
			else {
				stream.WriteLine("desired[key] = nil");
			}

			stream.Indent--;
			stream.WriteLine("}");
			stream.WriteLine("desired.to_json");

			stream.Indent--;
			stream.WriteLine("end");
		}

		private static void OutputGetMessageMethod(RBClass cls, CodeWriter stream)
		{
			stream.WriteLine("def get_message");
			stream.Indent++;
			stream.WriteLine("data = {");
			stream.Indent++;

			foreach (var t in cls.Telemetries) {
				stream.WriteLine(t.Name + ": @" + t.Name + ",");
			}

			stream.Indent--;
			stream.WriteLine("}.to_json");
			stream.WriteLine("message = AzureIoT::Message.new(data)");

			foreach (var s in cls.States) {
				stream.WriteLine("message.add_property(\"" + s.Name + "\", get_" + s.Name.ToLowerInvariant() + "())");
			}

			stream.WriteLine("return message");
			stream.Indent--;
			stream.WriteLine("end");
		}

		private static void OutputMeasureMethod(RBClass cls, CodeWriter stream)
		{
			stream.WriteLine("def measure");
			stream.Indent++;

			foreach (var t in cls.Telemetries) {
				stream.WriteLine("@" + t.Name + " = rand()");
			}

			stream.Indent--;
			stream.WriteLine("end");
		}
	}

	public class RBClass
	{
		public string Name { get; set; }

		public List<DTInterfaceContent> Telemetries { get; set; } = new List<DTInterfaceContent>();
		public List<DTInterfaceContent> States { get; set; } = new List<DTInterfaceContent>();
		public List<DTInterfaceContent> Commands { get; set; } = new List<DTInterfaceContent>();
		public List<DTInterfaceContent> Properties { get; set; } = new List<DTInterfaceContent>();
	}

	static class SymbolConverter
	{
		public static string ToLowerCaseUnderbar(this string value)
		{
			var result = new List<char>();
			foreach (var c in value) {
				if (Char.IsUpper(c)) {
					result.Add('_');
					result.Add(Char.ToLower(c));
				}
				else {
					result.Add(c);
				}
			}
			return new string(result.ToArray()).Trim('_');
		}
	}
}
