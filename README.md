# SCREW.Voice

SCREW.Voice — UDP server-client for realtime voice chat in C#.

The library uses the NAudio package to transmit and receive audio.

## Features

- **Ease of use**: Easily create voice chats on both server and client sides.
- **High-quality sound**: Supports high-quality audio.
- **Customization**: Adapt it to fit your projects.

## Usage Example
### Server Creation

```csharp
Server voiceServer = new Server(TimeSpan.FromSeconds(1), 8888);
voiceServer.Start();
```
### Client Creation
```csharp
ChatSettings settings = new ChatSettings()
{
    ipAddress = "127.0.0.1",
    port = 8888,
    uid = "test",
    deviceSettings = new DeviceSettings()
};
settings.deviceSettings.SetBufferMilliseconds(50);

Client voiceClient = new Client(settings);
```
