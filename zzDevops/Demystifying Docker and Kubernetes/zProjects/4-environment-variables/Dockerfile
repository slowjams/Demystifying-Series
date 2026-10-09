FROM node:22-alpine

WORKDIR /app

COPY package*.json .

RUN npm ci

COPY . .

# ENV can be overriden by docker run -e PORT=5001
ENV PORT=5000    
ENV APP_NAME="My App"

CMD ["node", "src/index.js"]