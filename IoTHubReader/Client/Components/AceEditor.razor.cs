using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace IoTHubReader.Client.Components
{
	public partial class AceEditor
	{
		static Dictionary<string, AceEditor> keyValues = new Dictionary<string, AceEditor>();

		public ElementReference EditorElement { get; set; }

		[Parameter]
		public EventCallback OnCreate { get; set; }

		[JSInvokable("AceEditor.OnCreateEntry")]
		public static Task OnCreateEntry(string id, object data)
		{
			if (keyValues.TryGetValue(id, out var editor)) {
				return editor.OnCreate.InvokeAsync(data);
			}
			return Task.CompletedTask;
		}

		[Parameter]
		public EventCallback OnChange { get; set; }

		[JSInvokable("AceEditor.OnChangeEntry")]
		public static Task OnChangeEntry(string id, object data)
		{
			if (keyValues.TryGetValue(id, out var editor)) {
				return editor.OnChange.InvokeAsync(data);
			}
			return Task.CompletedTask;
		}

		AceSession session;

		public AceSession getSession()
		{
			if (session == null)
				session = new AceSession(jsRuntime, EditorElement.Id);
			return session;
		}

		public ValueTask<string> getValue()
		{
			return jsRuntime.InvokeAsync<string>("AceEditor.getValue", EditorElement.Id);
		}

		public void setValue(string value)
		{
			jsRuntime.InvokeVoidAsync("AceEditor.setValue", EditorElement.Id, value);
		}

		public void moveCursorTo(int x, int y)
		{
			jsRuntime.InvokeVoidAsync("AceEditor.moveCursorTo", EditorElement.Id, x, y);
		}

		public void setTheme(string v)
		{
			jsRuntime.InvokeVoidAsync("AceEditor.setTheme", EditorElement.Id, v);
		}

		public void setShowInvisibles(bool v)
		{
			jsRuntime.InvokeVoidAsync("AceEditor.setShowInvisibles", EditorElement.Id, v);
		}

		public void gotoLine(int x, int y)
		{
			jsRuntime.InvokeVoidAsync("AceEditor.gotoLine", EditorElement.Id, x, y);
		}

		public void setReadOnly(bool v)
		{
			jsRuntime.InvokeVoidAsync("AceEditor.setReadOnly", EditorElement.Id, v);
		}

		public void resize()
		{
			jsRuntime.InvokeVoidAsync("AceEditor.resize", EditorElement.Id);
		}
	}

	public class AceSession
	{
		private IJSRuntime jsRuntime;
		private object sessionId;

		public AceSession(IJSRuntime jsRuntime, object sessionId)
		{
			this.jsRuntime = jsRuntime;
			this.sessionId = sessionId;
		}

		public void setMode(string mode)
		{
			jsRuntime.InvokeVoidAsync("AceSession.setMode", sessionId, mode);
		}

		public void setTabSize(int size)
		{
			jsRuntime.InvokeVoidAsync("AceSession.setTabSize", sessionId, size);
		}

		public void setUseSoftTabs(bool value)
		{
			jsRuntime.InvokeVoidAsync("AceSession.setUseSoftTabs", sessionId, value);
		}

		AceDocument document;

		public AceDocument getDocument()
		{
			if (document == null)
				document = new AceDocument(jsRuntime, sessionId);
			return document;
		}
	}

	public class AceDocument
	{
		private IJSRuntime jsRuntime;
		private object documentId;

		public AceDocument(IJSRuntime jsRuntime, object documentId)
		{
			this.jsRuntime = jsRuntime;
			this.documentId = documentId;
		}

		public void setValue(string value)
		{
			jsRuntime.InvokeVoidAsync("AceDocument.setValue", documentId, value);
		}
	}
}
