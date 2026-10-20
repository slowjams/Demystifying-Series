#!/bin/sh

# ./traffic-gen.sh "localhost:8080/api" 0.5
# ./traffic-gen.sh "color-api-clusterip/api" 0.5

if [ "$#" -lt 2 ]; then
    echo "Usage: $0 <target> <interval-in-seconds>"
    exit 1
fi

TARGET=$1
INTERVAL=$2

echo "Sending requests to $TARGET every $INTERVAL seconds."

while true; do
    REQUEST_TIME=$(date +"%Y-%m-%d %H:%M:%S")
    RESPONSE=$(curl -s "$TARGET")

    echo "[$REQUEST_TIME] $RESPONSE"

    sleep $INTERVAL
done
