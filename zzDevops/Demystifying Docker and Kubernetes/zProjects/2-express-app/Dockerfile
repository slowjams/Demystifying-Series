FROM node:22

WORKDIR /app

COPY package*.json .

# RUN happens when you build the image, by default RUN uses a shell because build steps often need shell functionality:
# CMD happens when you start a container
RUN npm ci

COPY src/index.js index.js

# only serves as documentation and can be used by some documentation tools
# it never open the port, only your application can make the port available to the outside world by doing `app.listen(3000)`
# it also instructs Docker to map the port to a random port on the host machine when using `docker run -P xxx` (note the uppercase P) by not specifiing the -p xxxx:xxxx, so now docker use EXPOSE to do port mapping like `docker run -p 1234:3000 xxx` where 1234 is a random port on the host machine chosen by Docker
EXPOSE 3000

CMD [ "node", "index.js" ]