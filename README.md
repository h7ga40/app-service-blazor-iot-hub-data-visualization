# Azure IoT Hub に送られたデータを表示する Blazor アプリケーション

Microsoftが提供している Node.js 版の[Azure IoT Hub のサンプルアプリ](https://github.com/Azure-Samples/web-apps-node-iot-hub-data-visualization)を、[Blazor](https://docs.microsoft.com/ja-jp/aspnet/core/blazor/?view=aspnetcore-3.1)で作ってみています。

## 使い方

### Azure IoT Hub の設定

[Azure Portal を使用して IoT Hub を作成する](https://docs.microsoft.com/ja-jp/azure/iot-hub/iot-hub-create-through-portal)

### Visual Studio 2019 でビルド

Visual Studio 2019 で「app-service-blazor-iot-hub-data-visualization.sln」を開いてビルドします。

### Visual Studio 2019 で公開

[Visual Studio を使用して Azure App Service に Web アプリを発行する](https://docs.microsoft.com/ja-JP/visualstudio/deployment/quickstart-deploy-to-azure?view=vs-2019)

### Azure App Service の設定

WebSocketを有効にする必要があります。<br>
Azure Portalで対象のApp Serviceを選択し、「構成」から、「全体設定」タブを選択し、「Web ソケット」をONにします。

### メッセージを送ってみる

「MessageSample」プロジェクトは、[Azure IoT Hub のドキュメント](https://docs.microsoft.com/ja-jp/azure/iot-hub/)で紹介されているサンプルアプリの1つで、C#を使ってAzure IoT Hubにメッセージを送ることが出来ます。

このプロジェクトを起動してメッセージを送ると、ブラウザで「Fetch data」のページを表示していると、メッセージが追加されます。

環境変数の```IOTHUB_DEVICE_CONN_STRING```にデバイスの接続文字列を設定する必要があります。

プロジェクトの設定にある「デバッグ」の「環境変数」で設定することが出来ます。

## アプリの説明

### 元のアプリの構成

元のアプリでは、サーバー側は Node.js の Express でWebサーバーを実行し、クライアント側はHTMLとJavaScriptで実行し、WebSocketで IoT Hub に送られたデータを Chart.js で表示するものになっています。

サーバー側では、クライアント側のWebSocketの接続を待つのと同時に、IoT Hub と接続しデバイスからのメッセージの受信も待っています。IoT Hubからメッセージを受信したときに接続されているWebSocketに送信する、中継ぎ処理を行っています。

### このアプリの構成

このアプリでは、Blazorを使って同じ構成のアプリを作りたいと思っています。

|機能|Node.js/HTML|Blazor|
|-|-|-|
|グラフ表示|[Chart.js](https://www.chartjs.org/)|[ChartJs.Blazor](https://www.iheartblazor.com/)|
|WebSocketクライアント|ブラウザのAPI|[System.Net.WebSockets.ClientWebSocket](https://docs.microsoft.com/ja-jp/dotnet/api/system.net.websockets.clientwebsocket?view=netcore-3.1)|
|WebSocketサーバー|[socket.io](https://socket.io/)|[System.Net.WebSockets.WebSocket](https://docs.microsoft.com/ja-jp/dotnet/api/system.net.websockets.websocket?view=netcore-3.1)|
|IoT Hub コンシューマー|[@azure/event-hubs](https://www.npmjs.com/package/@azure/event-hubs)の```EventHubClient```クラス|[Azure.Messaging.EventHubs](https://www.nuget.org/packages/Azure.Messaging.EventHubs/)の```EventHubConsumerClient```クラス|

### ユーザーシークレット

接続文字列の保存に[ユーザーシークレット](https://docs.microsoft.com/ja-jp/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows)を使っています。

Azure App Servie に公開した場合は、[Azure Key Vault](https://docs.microsoft.com/ja-jp/aspnet/core/security/key-vault-configuration?view=aspnetcore-3.1)に保存して運用します。
