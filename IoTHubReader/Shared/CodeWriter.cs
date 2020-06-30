using System;
using System.IO;

namespace IoTHubReader.Shared
{
	public class CodeWriter : StringWriter
	{
		int indent;
		string indentText;

		public int Indent {
			get { return indent; }
			set {
				indent = value;
				indentText = "";
				for (int i = 0; i < indent; i++) {
					indentText += IndentText;
				}
			}
		}
		public string IndentText { get; set; } = "  ";

		public new void WriteLine(string line)
		{
			var lines = line.Split(base.NewLine);
			foreach (var l in lines) {
				base.Write(indentText);
				base.WriteLine(l);
			}
		}
	}
}