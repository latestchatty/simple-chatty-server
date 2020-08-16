#!/bin/bash
set -euxo pipefail
gzip publish/SimpleChattyServer
aws s3 cp publish/SimpleChattyServer.gz s3://simple-chatty-server/ --acl public-read
