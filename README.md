# Azure IoT Hub �ɑ���ꂽ�f�[�^��\������ Blazor �A�v���P�[�V����

Microsoft���񋟂��Ă��� Node.js �ł�[Azure IoT Hub �̃T���v���A�v��](https://github.com/Azure-Samples/web-apps-node-iot-hub-data-visualization)���A[Blazor](https://docs.microsoft.com/ja-jp/aspnet/core/blazor/?view=aspnetcore-3.1)�ō���Ă݂Ă��܂��B

## �g����

### Azure IoT Hub �̐ݒ�

[Azure Portal ���g�p���� IoT Hub ���쐬����](https://docs.microsoft.com/ja-jp/azure/iot-hub/iot-hub-create-through-portal)

### Visual Studio 2019 �Ńr���h

Visual Studio 2019 �Łuapp-service-blazor-iot-hub-data-visualization.sln�v���J���ăr���h���܂��B

### Visual Studio 2019 �Ō��J

[Visual Studio ���g�p���� Azure App Service �� Web �A�v���𔭍s����](https://docs.microsoft.com/ja-JP/visualstudio/deployment/quickstart-deploy-to-azure?view=vs-2019)

### Azure App Service �̐ݒ�

WebSocket��L���ɂ���K�v������܂��B<br>
Azure Portal�őΏۂ�App Service��I�����A�u�\���v����A�u�S�̐ݒ�v�^�u��I�����A�uWeb �\�P�b�g�v��ON�ɂ��܂��B

### ���b�Z�[�W�𑗂��Ă݂�

�uMessageSample�v�v���W�F�N�g�́A[Azure IoT Hub �̃h�L�������g](https://docs.microsoft.com/ja-jp/azure/iot-hub/)�ŏЉ��Ă���T���v���A�v����1�ŁAC#���g����Azure IoT Hub�Ƀ��b�Z�[�W�𑗂邱�Ƃ��o���܂��B

���̃v���W�F�N�g���N�����ă��b�Z�[�W�𑗂�ƁA�u���E�U�ŁuFetch data�v�̃y�[�W��\�����Ă���ƁA���b�Z�[�W���ǉ�����܂��B

���ϐ���```IOTHUB_DEVICE_CONN_STRING```�Ƀf�o�C�X�̐ڑ��������ݒ肷��K�v������܂��B

�v���W�F�N�g�̐ݒ�ɂ���u�f�o�b�O�v�́u���ϐ��v�Őݒ肷�邱�Ƃ��o���܂��B

## �A�v���̐���

### ���̃A�v���̍\��

���̃A�v���ł́A�T�[�o�[���� Node.js �� Express ��Web�T�[�o�[�����s���A�N���C�A���g����HTML��JavaScript�Ŏ��s���AWebSocket�� IoT Hub �ɑ���ꂽ�f�[�^�� Chart.js �ŕ\��������̂ɂȂ��Ă��܂��B

�T�[�o�[���ł́A�N���C�A���g����WebSocket�̐ڑ���҂̂Ɠ����ɁAIoT Hub �Ɛڑ����f�o�C�X����̃��b�Z�[�W�̎�M���҂��Ă��܂��BIoT Hub���烁�b�Z�[�W����M�����Ƃ��ɐڑ�����Ă���WebSocket�ɑ��M����A���p���������s���Ă��܂��B

### ���̃A�v���̍\��

���̃A�v���ł́ABlazor���g���ē����\���̃A�v������肽���Ǝv���Ă��܂��B

|�@�\|Node.js/HTML|Blazor|
|-|-|-|
|�O���t�\��|[Chart.js](https://www.chartjs.org/)|[ChartJs.Blazor](https://www.iheartblazor.com/)|
|WebSocket�N���C�A���g|�u���E�U��API|[System.Net.WebSockets.ClientWebSocket](https://docs.microsoft.com/ja-jp/dotnet/api/system.net.websockets.clientwebsocket?view=netcore-3.1)|
|WebSocket�T�[�o�[|[socket.io](https://socket.io/)|[System.Net.WebSockets.WebSocket](https://docs.microsoft.com/ja-jp/dotnet/api/system.net.websockets.websocket?view=netcore-3.1)|
|IoT Hub �R���V���[�}�[|[@azure/event-hubs](https://www.npmjs.com/package/@azure/event-hubs)��```EventHubClient```�N���X|[Azure.Messaging.EventHubs](https://www.nuget.org/packages/Azure.Messaging.EventHubs/)��```EventHubConsumerClient```�N���X|

### ���[�U�[�V�[�N���b�g

�ڑ�������̕ۑ���[���[�U�[�V�[�N���b�g](https://docs.microsoft.com/ja-jp/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows)���g���Ă��܂��B

Azure App Servie �Ɍ��J�����ꍇ�́A[Azure Key Vault](https://docs.microsoft.com/ja-jp/aspnet/core/security/key-vault-configuration?view=aspnetcore-3.1)�ɕۑ����ĉ^�p���܂��B
