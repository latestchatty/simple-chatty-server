# SimpleChattyServer

This service powers `winchatty.com`.

## Deployment

- If a change to `appsettings.json` is needed, set it in Parameter Store in the key `/SimpleChattyServer/config`.
- In the `build` directory, run `./publish.sh` and then `./upload.sh`.
- In the `terraform` directory, run `terraform taint aws_instance.simple_chatty_server` and then `terraform apply`.
