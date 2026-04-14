## Demystifying Docker







============================================================================================================================================================================================

```bash
# frequentlt used commands
docker-compose -f docker-compose-dev.yml up
docker-compose -f docker-compose-dev.yml up --build
docker-compose -f docker-compose-dev.yml down

docker rm -f $(docker ps -aq)
docker logs -f productapp
docker network inspect bridge
```


Issue: the command 'docker' could not be found in this wsl 1 distro in Ubuntu
Solution:  in powshell `wsl -l -v` to get the distribution name which is most likely "ubuntu" then run `wsl --set-version ubuntu 2`

## What's a Docker Image

`docker run image` is a combination of `docker create` and `docker start`

```js
docker create Busybox // create a container (say id is abc123) whose status is "Created"
docker start abc123   // start the container the status is "running" and then "exit" (status might always be running depends on the nature of the imag
```

An image contains a small filesytem (e.g container home, bin, proc, root, et) and a default startup command. When do `docker create`, this image gets copied to your hard disk, that's all.

When you run `docker start`, the startup commands run so we have a running process.

Let's say you do `docker run Busybox echo hi there` where `echo hi there` is the command to overide the default command (the default command is not important to us). So the container run and print "hi there" then exit, and this new command is baked into the container. So if you start container again, it will print "hi there" again and exit

So a container (not just a running process) is consist of

1. Running process that executes a command
2. Kernel (host's OS)
3. portion of hard drive (used as container's file system), network, cpu, those resource are divided by Linux namespace


## Stop/Kill an Container

```js
docker stop  // send a SIGTERM signal, which allows you do a little bit clean-up (e.g flush buffer to save file etc) in your codebase when listen to this signal

docker kill  // send a SGKILL signal, which is like you have to shut down and you are not allowed to do any additional work
```

when you do `docker stop` and the container is not stoppped for 10 sec, the docker will automatically issue `docker kill`.


## Execute an Command on a Container

```js
// say you already have redis container running and want to use its cli
docker exec -it abc1234 redis-cli

docker exec -it abc1234 sh // run shell command. Container already runs before docker exec, so the default command has been run, if you don't want default command to run do the follwing

docker run -it redis sh // overwrite the default command so no other process will be running except shell, can be useful in some scenario such as below
docker run -it reactAppImage npm run test
```

Every process has three data stream i.e `STDIN` / `STDOUT` / `STDERR`. When a process is running in a container, by default terminal is connected with `STDOUT` stream of the process running get `STDIN` stream added when `i` is used. `-t` is used for formatted input operations (need to learn Linux hard to know what's tty)

so what does `-t` do? This is also best illustrated by example:

`docker run -e MYSQL_ROOT_PASSWORD=123 -i mariadb mysql -u root -p` will give you a password prompt. If you type the password, the characters are printed visibly but not masked

`docker run -i alpine sh` will give you an empty line. If you type a command like `ls` you get an output, but you will not get a prompt or colored output.

In the last two cases, you get this behavior because mysql as well as shell were not treating the input as a tty and thus did not use tty specific behavior like asking the input or coloring the output.

you can use `docker attach` to attach your terminal's standard input, output, and error

## Docker File

```js
FROM alpine  // Step 1

RUN apk add --update redis   // Step 2

CMD["redis-server"]  // Step 3
```

note that each step creates an intermittent running container then this container is removed and its filesystem is used to create a new image for next step.
for example, after Step 1, an intermittent image is created, and at Step 2, an intermittent container is created by the image at Step 1, and the container runs `apk add --update redis`, which adds a redis related files in the container's filiesystem which is used to create an imtermittent image for the next instruction and this intemittent container is removed too. So the image created at Step 3 is the final image to be used

```js
docker commit -c "CMD 'redis-server'" containerId  // build a new image based on container (container's filesystem will be used to create the image)

docker build --progress=plain .

docker build -f Dockerfile.dev .
```

```yml
FROM node:14-alpine

#RUN npm config set strict-ssl=false

WORKDIR /usr/app  # set the working directory. If the WORKDIR doesn't exist, it will be created even if it's not used in any subsequent Dockerfile instruction

COPY ./package.json ./  # if you remove this line and do move COPY ./ ./ here, it breaks cache when you modify the app e.g index.js

RUN npm install

COPY ./ ./

CMD ["npm", "start"]
```

## The Two forms

#### Shell Form:

Commands are written without `[]` brackets and are run by the container's shell, such as /bin/sh -c. 

```yml
FROM alpine:latest

# /bin/sh -c 'echo $HOME'
RUN echo $HOME

# /bin/sh -c 'echo $PATH'
CMD echo $PATH
```

Depending on the shell, commands will execute as child processes of the shell, which has some potentially negative consequences at the cost of some features

The main thing you lose with the exec form is all the useful shell features: sub commands, piping output, chaining commands, I/O redirection, and more. These kinds of commands are only possible with the shell form:

```yml
FROM ubuntu:latest

# Shell: run a speed test
RUN apt-get update \
 && apt-get install -y wget \
 && wget -O /dev/null http://speedtest.wdc01.softlayer.com/downloads/test10.zip \
 && rm -rf /var/lib/apt/lists/*

# Shell: output the default shell location
CMD which $(echo $0)
```

#### Exec Form:

Commands are written with `[]` brackets and (its executable) are run directly, not through a shell

```yml
FROM alpine:latest

RUN ["pwd"]

CMD ["sleep", "1s"]
```

This removes a potentially extra child process in the process tree, which may be desirable

Most shells do not forward process signals to child processes, which means the SIGINT generated by pressing CTRL-C may not stop a child process, This is the main reason to use the exec form for both `ENTRYPOINT` and `CMD`.

=======================================================================================================================================


## Docker-Compose

`docker run myImage` = `docker-compose up`

`docker build .` + `docker run myImage` = `docker-compose up --build`

note that by default, `docker-compose up` creates default network for its container to communicate with each others

```yml
version: '3'

services:
  redis-server:
    image: 'redis'
  node-app:
    restart: always
    #restart: "no"            // default, note that no has to be quoted because yml interpret unquoted no as false, which is different to "no" string
    #restart: always          // restart container for any reason such as exit(0) and exit(1,2, ...). If it's manually stopped, it's restated only when Docker daemon restarts 
                              # or the container itself is manually restarted
    #restart: on-failure      // restart for exit(1,2, ...) only
    #restart: unless-stopped  // similar to always, except that when the container is stopped, it isn't restarted even after Docker daemon restarts
    build: .
    ports:
      -"4001:8081"
```

```yml
# Dockerfile.dev

FROM node:16-alpine

USER node

RUN mkdir -p /home/node/app

WORKDIR /home/node/app

COPY --chown=node:node ./package.json ./

RUN npm install

COPY --chown=node:node ./ ./

CMD ["npm", "start"]
```

```bash
docker build -t frontend -f Dockerfile.dev .

##---------------- note that if you change the source file, the UI/test suite won't update because when we create the image, we create a snapshot of the file system at the moment, to do live updating, we need to use volumn to tell docker to look for original file/folder in the host rather than inside the container
docker run -it frontend ## run with default CMD which is npm start           
docker run -it frontend npm run test
##----------------
```

```yml
# we use npx create-react-app to create a project on host then delete node_modules to reduce the build content of the image
# we want container "listen" to the source file change (e.g app.js) so the react app running in the container can automatically refresh
# `-v /app/node_modules` is to bookmarking node_modules folder so it tell container to use its own node_modules folder, the reason we do it is because we don't need to have node_modules in our host machine, docker instance can download the package using package.json and download packages into its own node_modules folder, but since we want docker instance watch the source code changes (changes are made on host's/developer's machine). So we use volumn command `-v $(pwd):/app`
# `$(pwd):/app` is to create a "reference" to tell container anything inside the app folder is pointed to the current working directory of the host. So  `-v pathOnHost:/pathInsideDocker ` is about using a volumn (because it contains colon, which means mapping between host and docker instance). `-v InsideDockerFolder` means "bookmarking", which tell docker don't map it to the host, use its own folder. Of course, bookmarking only make senses when you are already using volumn, so you will see two `-v` tags, one for using volumn (with colon), the other for bookmarking (without colon).
docker run -p 3000:3000 -v /home/app/node_modules -v $(pwd):/app imageId 
```

`/mnt/wsl/docker-desktop-data/` ? what is this?

Volumn storage location for wsl is `docker-desktop-data/data/docker/volumes`, e.g `-v myVolumn:/app` will create a `docker-desktop-data/data/docker/volumes/myVolumn`. Note that you can also do `docker volume create --name myVolumn` to create aa volume in advance but this is obsolete, `-v myVolumn:/app` will automatically create the volumn for you.
You can also specify a path like `docker run -v $(pwd)/myFolder:/app imageId`, in this case, there won't be volumn file created at `docker-desktop-data/data/docker/volumes` and in case "myFolder" doesn't exist on the host'disc then docker will automatically create it for you.

```yml
## docker-compose.yml, use docker-compose file to simplify volumn
version: '3'
services:
  web:
    build:
      context: .  # "." mean current directory, if your custom docker file is nested in bar folder, then it is "./bar"
      dockerfile: Dockerfile.dev
    ports:
      - "3000:3000"
    volumes:
      - /home/node/app/node_modules
      - .:/home/node/app

```

```bash
docker-compose up  ## run react app with live updating, let's the container id is abc123

docker exec -it abc123 npm run test  ## to live update tests, you shoud add a section ("test") in the compose file below, but you can also do in this way
```

```yml
## docker-compose.yml, use docker-compose file to simplify volumn
version: '3'
services:
  web:
    build:
      context: .  # "." mean current directory, if your custom docker file is nested in bar folder, then it is "./bar"
      dockerfile: Dockerfile.dev
    ports:
      - "3000:3000"
    volumes:
      - /home/node/app/node_modules
      - .:/home/node/app
  test:
    build:
      context: .
      dockerfile: Dockerfile.dev
    volumes:
      - /home/node/app/node_modules
      - .:/home/node/app
    command: ["npm", "run", "test"]  # override the default startup CMD in the docker file
```

let's say you want `docker-compose up` on above compose file, and you want to use `docker attach runTestContainerID` and think you can use `STDIN` of the running "npm run test" process and type commands but you can't. The reason is `docker attach` only attach to the "primary process" which is "npm", process "npm" forks a new process to process the arguments "run test" so there are actually two processes in the background.



## Docker Networks

```sh
docker network ls
# NETWORK        ID       NAME     DRIVER SCOPE
# 3826803e0a8c   bridge   bridge   local        <----------------bridge is the default network that docker will be using
# 0e643a56cce6   host     host     local
# 9058e96d6113   none     null     local
```

if you run an mysql (`docker run -d --name mysql -v productdata:/var/lib/mysql -e MYSQL_ROOT_PASSWORD=mysecret -e bind-address=0.0.0.0 mysql:8.0.0`) and asp.net app (`docker run -d --name productapp -p 3000:80 apress/exampleapp`) instances without specifying the network as:
```sh
docker network inspect bridge
#...
#"Containers": {
# "1be5d5609e232335d539b29c49192c51333aaea1fd822249342827456aefc02e": {
# "Name": "productapp",
# "EndpointID": "3ff7acdaf3cce9e77cfc7156a04d6fa7bf4b5ced3fd13",
# "MacAddress": "02:42:ac:11:00:03",
# "IPv4Address": "172.17.0.3/16",
# "IPv6Address": ""
# },
# "9dd1568078b63104abc145840df8502ec2e171f94157596236f00458ffbf0f02": {
# "Name": "mysql",
# "EndpointID": "72b3df290f3fb955e1923b04909b7f27fa3854d8df583",
# "MacAddress": "02:42:ac:11:00:02",
# "IPv4Address": "172.17.0.2/16", <------------------------------------------------
# "IPv6Address": ""
# }
#},
#...

# to make productapp talks to the mysql instance, you have to provide the private ip address of mysql in bridge network
# note that you cannot do `docker run -d --name productapp -p 3500:80 -e DBHOST=mysql apress/exampleapp` since bridge network doesn't support DNS, only customed created network
# support DNS so you can use the server name like `mysql` directly as you will see in the next example
docker run -d --name productapp -p 3500:80 -e DBHOST=172.17.0.2 apress/exampleapp  # check the Exampleapp source code
```

To create customer networks
```sh
docker network create frontend
docker network create backend
# NETWORK       ID        NAME    DRIVER SCOPE
# fa1bc701b306  bridge    bridge  local
# 778d9eb6777a  backend   bridge  local
# 2f0bb28d5716  frontend  bridge  local
# 0e643a56cce6  host      host    local
# 9058e96d6113  none      null    local

docker run -d --name mysql -v productdata:/var/lib/mysql --network=backend -e MYSQL_ROOT_PASSWORD=mysecret -e bind-address=0.0.0.0 mysql:8.0.0
docker create --name productapp -e DBHOST=mysql -e MESSAGE="1st Server" --network backend apress/exampleapp
docker network connect frontend productapp
docker start productapp1

docker run -d --name loadbalancer --network frontend -v "$(pwd)/haproxy.cfg:/usr/local/etc/haproxy/haproxy.cfg" -p 3000:80 haproxy:1.7.0
```

Compose always creates a network. It never uses the "default bridge network". If you don't manually declare networks:, Compose creates a network named "default" for you, but in the language of the documentation, this is a "user-defined bridge network". That is, Compose's default behavior is more or less to do
```sh
docker network create default
docker run --net=default ...
```



## Demystifying Kubernetes

`minikube`: use to create and manage the VM (also called a `node` which is K8s cluster)
`kubectl` : use for managing containers in the node

Object Types:
* StatefulSet
* ReplicaController
* Pod
* Service : Sets up networkin gin a K8s cluster
  * ClusterIP
  * NodePort  <----------------expose a container to the outside world
  * LoadBalancer
  * Ingress