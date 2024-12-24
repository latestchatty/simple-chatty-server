# SimpleChattyServer

This service powers `winchatty.com`. [View the API documentation](https://github.com/latestchatty/simple-chatty-server/blob/master/doc/api.md).

## Development

- Use the VSCode devcontainer.
- Edit `src/appsettings.json` and fill in, at minimum, the `SharedLogin` and `Storage` sections.
- In the `build` directory, run `./clean.sh && ./debug.sh`.

## Deployment

- In the `build` directory, run `./clean.sh && ./publish.sh`.
- Make a deployment directory.
- Copy `build/publish/*.exe` and `*.dll` to the deployment directory.
- Copy your customized `appsettings.json` to the deployment directory.
- Run `SimpleChattyServer.exe`. Use `sc.exe` to set it up as a Windows service.
