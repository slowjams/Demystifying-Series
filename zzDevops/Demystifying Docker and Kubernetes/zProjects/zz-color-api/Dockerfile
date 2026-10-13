FROM node:22-alpine3.20

WORKDIR /app
COPY package*.json .
RUN npm ci
COPY src src
CMD ["npm", "start"]

# kubectl run color-api --image=slowjams/color-api:1.1.0 # --dry-run=client -o yaml
# docker run -d -p 8080:80 slowjams/color-api:1.1.0