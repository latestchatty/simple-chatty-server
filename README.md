# SimpleChattyServer

This service powers `winchatty.com`.

## Development

- In the `build` directory, run `./debug.sh`.

## Deployment

- If a change to the production `appsettings.json` is needed, set it in Parameter Store in the key `/SimpleChattyServer/config`.
- In the `build` directory, run `./clean.sh && ./publish.sh && ./upload.sh`.
- In the `terraform` directory, run `terraform taint aws_instance.simple_chatty_server && terraform apply`.
