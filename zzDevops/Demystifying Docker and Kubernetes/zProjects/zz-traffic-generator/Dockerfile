FROM alpine:3.20

WORKDIR /app

RUN apk add --no-cache curl

COPY traffic-gen.sh traffic-gen.sh

RUN chmod +x traffic-gen.sh

ENTRYPOINT [ "./traffic-gen.sh" ]

# docker build -t slowjams/traffic-gen:1.0.0 .
# docker run slowjams/traffic-gen:1.0.0 "localhost:8080/api" 0.5
