window.AceEditor = {
	keyValues: {},
	createEditor: function (elementId, editorElement) {
		editorElement.id = elementId;
		if (!window.AceEditor.keyValues[elementId]) {
			var editor = ace.edit(editorElement);
			window.AceEditor.keyValues[elementId] = editor;

			editor.on("change", (data) => {
				DotNet.invokeMethodAsync('IoTHubReader.Client', 'AceEditor.OnChangeEntry', elementId, data);
			});

			DotNet.invokeMethodAsync('IoTHubReader.Client', 'AceEditor.OnCreateEntry', elementId, {});
		}
	},
	getValue: function (elementId) {
		var ret = "";
		editor = window.AceEditor.keyValues[elementId];
		if (editor) {
			ret = editor.getValue();
		}
		return ret;
	},
	setValue: function (elementId, text) {
		editor = window.AceEditor.keyValues[elementId];
		if (editor) {
			editor.setValue(text);
		}
	},
	moveCursorTo: function (elementId, x, y) {
		editor = window.AceEditor.keyValues[elementId];
		if (editor) {
			editor.moveCursorTox, y();
		}
	},
	setTheme: function (elementId, v) {
		editor = window.AceEditor.keyValues[elementId];
		if (editor) {
			editor.setTheme(v);
		}
	},
	setShowInvisibles: function (elementId, v) {
		editor = window.AceEditor.keyValues[elementId];
		if (editor) {
			editor.setShowInvisibles(v);
		}
	},
	gotoLine: function (elementId, v1, v2) {
		editor = window.AceEditor.keyValues[elementId];
		if (editor) {
			editor.gotoLine(v1, v2);
		}
	},
	setReadOnly: function (elementId, v) {
		editor = window.AceEditor.keyValues[elementId];
		if (editor) {
			editor.setReadOnly(v);
		}
	},
	resize: function (elementId) {
		editor = window.AceEditor.keyValues[elementId];
		if (editor) {
			editor.resize();
		}
	}
}

window.AceSession = {
	setMode: function (sessionId, mode) {
		editor = window.AceEditor.keyValues[sessionId];
		if (editor) {
			var session = editor.getSession();
			session.setMode(mode);
		}
	},
	setTabSize: function (sessionId, size) {
		editor = window.AceEditor.keyValues[sessionId];
		if (editor) {
			var session = editor.getSession();
			session.setTabSize(size);
		}
	},
	setUseSoftTabs: function (sessionId, value) {
		editor = window.AceEditor.keyValues[sessionId];
		if (editor) {
			var session = editor.getSession();
			session.setUseSoftTabs(value);
		}
	}
}

window.AceDocument = {
	setValue: function (documentId, value) {
		editor = window.AceEditor.keyValues[documentId];
		if (editor) {
			var doc = editor.getSession().getDocument();
			doc.setValue(value);
		}
	}
}
