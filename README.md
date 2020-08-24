# SimpleChattyServer

This service powers `winchatty.com`. [View the API documentation](https://github.com/latestchatty/simple-chatty-server/blob/master/doc/api.md).

## Development

- Edit `src/appsettings.json` and fill in, at minimum, the `SharedLogin` and `Storage` sections.
- In the `build` directory, run `./debug.sh`.

## Deployment

- If a change to the production `appsettings.json` is needed, set it in Parameter Store in the key `/SimpleChattyServer/config`.
- In the `build` directory, run `./clean.sh && ./publish.sh && ./upload.sh`.
- In the `terraform` directory, run `terraform taint aws_instance.simple_chatty_server && terraform apply`.

## Updating an existing deployment

```
systemctl stop simple-chatty-server.service && \
mv SimpleChattyServer SimpleChattyServer.bak && \
aws s3 cp s3://simple-chatty-server/SimpleChattyServer.gz /opt/simple-chatty-server/ && \
gunzip /opt/simple-chatty-server/SimpleChattyServer.gz && \
chmod +x /opt/simple-chatty-server/SimpleChattyServer && \
systemctl start simple-chatty-server.service && \
journalctl -f | grep Scrape
```
