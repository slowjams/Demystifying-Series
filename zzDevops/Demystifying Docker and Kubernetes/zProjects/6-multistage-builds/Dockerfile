FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json .
RUN npm ci

COPY src src
COPY tsconfig.json .

# produce the final build artifacts dist/index.js
RUN npm run build

# intermediate image like "build" will not be destroyed after the final image is built
# intermediate image "build" image will be in docker build cache which can be cleaned with `docker builder prune`

FROM node:22-alpine AS deps
WORKDIR /app
COPY package*.json .
RUN npm ci --only=production

FROM gcr.io/distroless/nodejs22

WORKDIR /app
COPY --from=deps /app/node_modules node_modules
COPY --from=build /app/dist dist

ENV PORT=3000

# no need to do `CMD ["node", "src/index.js" ]` as "node" is the default entrypoint for distroless/nodejs22 image
CMD ["dist/index.js" ]