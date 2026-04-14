FROM node:16-alpine as builder
USER node
RUN mkdir -p /home/node/app
WORKDIR /home/node/app
COPY --chown=node:node ./package.json ./
RUN npm install
COPY --chown=node:node ./ ./
## note that following instructin is "build", not "start"
RUN npm run build

From nginx
COPY --from=builder /home/node/app/build /usr/share/nginx/html
