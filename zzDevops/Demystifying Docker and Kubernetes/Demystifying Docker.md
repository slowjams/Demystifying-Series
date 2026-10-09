## Demystifying Docker

============================================================
```bash
docker build -t xxx:5000 .
docker run -d -p 3000:3000 --name xxx image
docker run --rm -it image sh 
docker exec -it RunningContianer sh
docker logs -f express-5001  # --follow, option keeps the command running and streams new log output as the container writes it.
docker run --env-file ".env.prod" -d -p 9000:9000 --name xxx iii


docker kill $(docker ps -aq)  # ps mean "process status"
docker rm $(docker ps -aq)
docker rmi $(docker images -q)

docker builder prune  # removes unused build cache.
docker image inspect gcr.io/distroless/nodejs22

## <----------------------------------------------------------------------------to be deleted
docker run -d --name mongodb mongodb/mongodb-community-server:7.0-ubuntu2204
docker exec -it mongodb mongosh

docker run --rm --name debugsh -it --network key-value-net mongodb/mongodb-community-server:7.0-ubuntu2204 mongosh mongodb://userFoo:passwordBar@mongodbContainerName/my-db

docker run --rm --name debugsh -it --network z10-compose_default mongodb/mongodb-community-server:7.0-ubuntu2204 mongosh mongodb://z10-compose-db-1

docker run -d --name backend --network key-value-net -p 3000:3000 key-value-backend

npm i --save-exact express@4.19.2 mongoose@8.5.1 body-parser@1.20.2
npm i --save-exact --save-dev nodemon@3.1.4
## <----------------------------------------------------------------------------to be deleted

echo 'export DOCKER_HOST=unix:///mnt/wsl/docker-desktop-bind-mounts/Ubuntu/docker.sock' >> ~/.bashrc
source ~/.bashrc
```
============================================================

Linux preprequisites:

==========================================================================  

```yml
# You open a terminal window:

$ tty        # tty means the controlling terminal of the current process.
/dev/pts/3   # pts stands for "Pseudo Terminal Slave", /dev/ptmx is PTY creator

bash process  # every Linux process starts with three standard file descriptors, and note that all fds map to same terminal
 |
 +-- STDIN  (fd 0)  ---> /dev/pts/3 
 |
 +-- STDOUT (fd 1)  ---> /dev/pts/3
 |
 +-- STDERR (fd 2)  ---> /dev/pts/3 
                                      
 /dev/tty
    |
    v
/dev/pts/3
```

====================================================================================

```
Process
+-----------------+
| FD 0 -----------+----------------------+
+-----------------+                      |
                                         v                       kernel calls driver's read function (write function for FD1)                  
                             Terminal device (/dev/pts/0)  <-----Terminal Driver --->  Kernel Terminal Object (live inside kernel)
                                                                                        +--------------------------+
                                                                                        | Input buffer             |
                                                                                        | Output buffer            |
                                                                                        | Terminal settings        |
                                                                                        | (echo, canonical mode)   |
                                                                                        +--------------------------+
```
when you type `A`:
```
Keyboard
    |
    v
Linux Kernel
    |
    v
Terminal driver
    |
    +--> stores 'A' in input buffer
```
No file descriptor is involved yet. Only when your program executes: `read(0, buf, 1)` kernel does:

1. Look up FD 0 in the process's file descriptor table.
2. See that FD 0 refers to /dev/pts/0.
3. Ask the terminal driver for data.
4. The terminal driver copies bytes from its input buffer into `buf`.

note that `/dev/pts/0` is a device node, which is a a special file, not a regular file (that store text data). This device node contains metadata that tells the kernel which **device driver** and which device instance it represents:
```
/dev/pts/0

+----------------------+
| File metadata        |
| type = character     |
| major = 136          |
| minor = 0            |
+----------------------+
```


====================================================================================

you may ask why doesn't writing overwrite the input? Because the terminal device internally has separate paths:
```
             /dev/pts/3

        +----------------+
Input   | input buffer   | <--- keyboard characters
        +----------------+
                |
                v
             read(fd 0)


        +----------------+
Output  | output buffer  | ---> screen
        +----------------+
                ^
                |
             write(fd 1)
```

The kernel checks the FD before calling the terminal driver. Suppose Bash does `read(0, buffer, 100);`:
```
Bash
 |
 | read(fd=0)
 v
Kernel
 |
 | lookup fd 0
 v
File object
 |
 | permission: READ
 v
PTY driver
```

This means:
* When bash wants to read your keyboard input, it reads from `/dev/pts/3` which is a kernel PTY slave device object.
* When bash wants to display something, it writes to /dev/pts/3.

A Terminal is a terminal emulator who jobs are "display text", "capture keyboard input" etc, it doesn't understand commands like cd, dir etc, which is managed by shell. The terminal uses `fork()` + `exec()` to create the shell process, so there are 2 Linux processes when you open a termnal, one if terminal process, the other is bash shell process.

When a Terminal started, it asked the kernel for a PTY pair, and the terminal owns the master
```
PTY Master <------> PTY Slave
     |                   |
     |                   |
Terminal App         /dev/pts/3
```

A terminal asks the kernel for a new PTY by opening: `open("/dev/ptmx")`, the kernel creates a PTY pair:
```
                 Kernel
PTY master    <============>    PTY slave
FD 10                          (/dev/pts/3)   

PTY master    <============>    PTY slave
FD 11                          (/dev/pts/4) 
```
The terminal writes to the PTY master first (not /dev/pts/3 first), and the kernel "sends" (a lot of steps involved such as usb driver etc) that input event appear as input on `/dev/pts/3`

```
[ Terminal Emulator ] <---> [ PTY Master ] <=== PTY Kernel Driver ===> [ PTY Slave ] <---> [ Interactive Program e.g. bash ]"
```

if you ask why not complete remove PTY Master as "[ Terminal Emulator ] <===> /dev/pts/3 <===> [ bash ]

The problem: If the Terminal Emulator and bash are both connected to /dev/pts/3, the kernel cannot tell who is the host and who is the client.
* When you type the letter 'a', the Terminal Emulator writes 'a' to /dev/pts/3.
* Who gets the letter 'a'?
* Because both processes are listening on /dev/pts/3, there is a race condition. The Terminal Emulator itself might read back the 'a' it just wrote before bash ever sees it!

==========================================================================    

```
You press Ctrl+C
        |
        v
Terminal emulator (user-space application, most written in C)
(Windows Terminal, GNOME Terminal, etc.)
        |
        v
TTY subsystem (Linux kernel)
        |
        v
Generate SIGINT signal
        |
        v
Foreground process group
        |
        v
bash / stopping program   (note, Ctrl+C is not always stoppping a program, programs need to decide what to do when receive SIGINT)
```

==========================================================================  

A container does not need a Linux kernel inside the image as it uses Host's OS, but it usually needs a Linux user-space filesystem because the application expects Linux libraries, tools, and directory structures. So when you pull nginx `docker pull nginx, the image contains something like::
```
nginx image
|
+-- Debian/Alpine filesystem
|
+-- nginx executable
|
+-- nginx configuration
|
+-- libraries
|
+-- shell tools
```

so a container contains Linux filesystem:
```
Container
 |
 +-- Linux filesystem
 |      |
 |      +-- shell
 |      +-- libraries
 |      +-- commands
 |
 +-- nginx process
```
after you run `docker exec -it web_server /bin/bash` (Docker creates a new process `bash`)
```
nginx container

PID 1: nginx
  |
  +--> stdout --> Docker logs

PID 15: bash
  |
  +--> stdin  <-- your keyboard
  |
  +--> stdout --> your terminal
```
=====================================   

`it` flag:

| Docker option | Analogy                                          |
| ------------- | ------------------------------------------------ |
| `bash`        | The driver                                       |
| `-t`          | Install a steering wheel                         |
| `-i`          | Connect the driver's hands to the steering wheel |
| `-it`         | A complete drivable car                          |

A driver (bash) without a steering wheel (TTY) cannot operate normally. A steering wheel without hands (-t without -i) also does nothing.

```
Your keyboard
      |
      v
 Docker STDIN (-i)
      |
      v
 Pseudo-terminal (-t)
      |
      v
 bash
      |
      v
 Output back to your screen
```

node image (`FROM node`)'s file system:
```yml
/
├── app   # WORKDIR /APP
│   └── index.html
│
├── bin
│   ├── bash
│   ├── ls
│   └── cat
│
├── boot
│
├── dev
│
├── etc
│   ├── passwd
│   ├── group
│   ├── hosts
│   ├── hostname
│   └── apt
│       └── sources.list
│
├── home
│
├── lib
│   ├── x86_64-linux-gnu
│   └── ...
│
├── lib64
│
├── media
│
├── mnt
│
├── opt
│
├── proc
│
├── root
│
├── run
│
├── sbin
│   ├── init
│   └── ...
│
├── srv
│
├── sys
│
├── tmp
│
└── usr
    └── local
        ├── bin
        │   ├── node       <-- Node executable
        │   ├── npm        <-- npm executable
        │   └── npx
        │
        ├── include
        │   └── node
        │
        ├── lib
        │   └── node_modules
        │       └── npm
        │
        └── share
            └── doc
```

so when you see Dockerfile olike:
```yml
FROM ubuntu
# ...
```
**note that the final image doesn't contain ubuntu Linux kernel**, what the image contain is the user space file system, the kernel is in the host (WSL for Windows)

===============================================================================================================

## WSL and Docker Desktop

```
Windows
│
└── WSL2 / virtualization
    │
    ↓
Linux kernel (aka WSL2 kernel is a Linux kernel specifically packaged and configured by Microsoft for WSL2)
    │
    ↓
    ├── Ubuntu
    │    └── Linux filesystem
    │       ├── /home
    │       ├── /usr
    │       ├── /etc
    │       └── ...
    │
    └── docker-desktop (docker-desktop isn't a normal Linux distro)
         └── Linux filesystem
             ├── /usr
             ├── /etc
             ├── /var
             ├── /run
             └── ...
```

Ubuntu and Docker Desktop are therefore separate WSL environments, even though they're using the same underlying WSL2/virtualization infrastructure.
That's why when you do `wsl -l -v`:
```
NAME              STATE           VERSION
Ubuntu            Running         2
docker-desktop    Running         2
```
===============================================================================================================

## Docker Daemon aka `dockerd`

```
Docker Engine
│
├── dockerd       ← Docker's main daemon
│
├── containerd    ← container lifecycle
│
└── runc          ← low-level container creation
```

```
You
 │
 │ docker run / docker build / docker ps
 ▼
Docker CLI
 │
 │ sends requests
 ▼
Docker Daemon (`dockerd`, like "car control system")
 │
 ├── manages networks
 ├── manages volumes
 ├── port publishing
 └── coordinates / requests containerd to create/stop containers etc
 │
 ▼
Container runtime (`containerd`, like "car engine")
 │
 ├── creates containers
 ├── starts/stops containers
 ├── builds images
 ├── downloads images
 ├── manages networks
 ├── manages volumes
 └── talks to the Linux kernel   <--------------------------------by runc


 runc
 │
 ├── create Linux namespaces
 │      ├── PID namespace
 │      ├── network namespace
 │      ├── mount namespace
 │      └── etc.
 │
 ├── configure cgroups
 │
 ├── configure mounts
 │
 └── start nginx process
```



# Existing Note==========================================================================================================================================================

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


Q: When inspecting an image with docker history, you see that one RUN layer adds 500 MB to the image size, while the very next RUN layer removes those files and shows a size of only 1 KB. What is the effect on the final image size?
```yml
FROM ubuntu

RUN apt-get update && apt-get install -y some-large-package # says package size is 500mb
RUN rm -rf /var/lib/apt/lists/*
```
A: The final image size still includes the 500 MB from the first RUN layer. Even though the next RUN removes the files, Docker image layers are immutable. Removing files in a later layer does not shrink previous layers. The final image looks like:
```
Image
 |
 +-- Layer 2 (+1 KB)
 |       deletes files
 |
 +-- Layer 1 (+500 MB)
         contains the files
```
The container filesystem appears to have the files removed, but the data still exists in Layer 1 (Docker needs Layer 1 because older images or containers may depend on it.). So: Final image size ≈ Base image + 500 MB + 1 KB

How to avoid this? Combine commands into one RUN layer:
```
RUN apt-get update \
    && apt-get install -y package \
    && rm -rf /var/lib/apt/lists/*
```


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
FROM ubuntu:latest

CMD ["echo", "hello"]  # <----------echo is in /usr/bin/echo, Linux process startup mechanism searches for echo using the PATH environment variable, no shell involved

# user can run "docker run ubuntu" or docker run ubuntu echo Bye" (override default cmd)
```

```yml
FROM ubuntu:latest

CMD echo hello  # use /bin/sh by default, which involed two process, main process is sh process and child process is echo process
```


This removes a potentially extra child process in the process tree, which may be desirable

Most shells do not forward process signals to child processes, which means the SIGINT generated by pressing CTRL-C may not stop a child process, This is the main reason to use the exec form for both `ENTRYPOINT` and `CMD`.

## CMD Vs ENTRYPOINT

**`ENTRYPOINT` is used to specify the main executable for the container, which is not intended to be overridden, while `CMD` is used to provide default arguments for that executable, which are easily overridden by the user at runtime.**

```yml
FROM ubuntu:latest

ENTRYPOINT ["echo"]   # user can override CMD but not ENTRYPOINT (unless  --entrypoint is used)

# user run `docker run ubuntu Hello`
```
the process is:
```yml
FROM alpine:3.20

# the order of CMD and ENTRYPOINT does not matter
ENTRYPOINT ["echo"]
CMD ["DEFAULT MESSAGE"]  # CMD will be part of argument array "appended" to  ENTRYPOINT

# execve(
#     "/bin/echo",
#     {
#         "echo",
#         "DEFAULT MESSAGE",
#         NULL
#     },
#     envp
# );
```
if you do `docker run --rm image "CUSTOM MESSAGE"`, then `CMD ["DEFAULT MESSAGE"]` is override as:
```yml
# execve(
#     "/bin/echo",
#     {
#         "echo",
#         "CUSTOM MESSAGE",
#         NULL
#     },
#     envp
# );
```

ENTRYPOINT + CMD together (most common):

```yml
ENTRYPOINT ["nginx"]

CMD ["-g", "daemon off;"]
```
`docker run nginx` runs `nginx -g "daemon off;"` internally 

=======================================================================================================================================


# Bind Mount and Named volumes

```
Docker mounts
├── Volume
│   └── Managed by Docker
│
└── Bind mount
    └── Uses a specific host filesystem path

# <-------------------------both use the -v / --volume syntax, a little bit confusion here
```

While bind mounts are excellent for development, Named Volumes generally the preferred choice for managing application data (like a database's files) in production environments because **Named volumes are platform-agnostic and managed by the Docker engine, decoupling the data from the host's specific directory structure. This makes the application more portable and easier to manage across different hosts**

```bash
# bind mount
docker run ... -v ./public:/app/public -v ./src:/app/src react-app  # Bind mount, React Development Server needs to modify file changes for hot reloading
```

```bash
# named mount
docker volume create website-data

docker run -d -p 3000:80 --name website-main -v website-data:/usr/share/nginx/html nginx:1.27.0   #  website-data is NOT host directory like bind mount
docker run -d -p 3001:80 --name website-readonly -v website-data:/usr/share/nginx/html:ro nginx:1.27.0  # -v SOURCE:DESTINATION:OPTIONS, ro mean read-only

# New empty named volume → Docker may initialize it from the image's existing directory. 

#  Nginx image
#  /usr/share/nginx/html/
#  ├── index.html
#  └── 50x.html
#         │
#         │ initial population
#         ▼
#   website-data
#   ├── index.html
#   └── 50x.html

# Existing/non-empty volume → its contents are used as-is.

docker volume inspect website-data
#[
#    {
#        "CreatedAt": "2026-08-14T14:22:45Z",
#        "Driver": "local",
#        "Labels": null,
#        "Mountpoint": "/var/lib/docker/volumes/website-data/_data",  <------------in Docker Engine's Linux filesystem
#        "Name": "website-data",
#        "Options": null,
#        "Scope": "local"
#    }
#]
```

=======================================================================================================================================

# Restart Policy

```bash
docker run --restart on-failure:3 busybox sh -c "sleep 3; exit 1"    # restart 3 times
docker run --restart on-failure:3 busybox sh -c "sleep 3; exit 0"    # won't restart, exit 0 mean "no error", so on-failure doesn't apply

docker run --restart always busybox sh -c "sleep 3; exit 0"          # will restart despite of exit 0, but if it is stopped manually,  dokcer won't restart it
                                                                     # but when Docker Engine is restarted, then this container will be restarted
docker run --restart unless-stopped busybox sh -c "sleep 3; exit 0"  # same as above except it won't be restarted after Docker Engine is restarted 
```

## Networking

By default, all container join **bridge** (`default`) network

```json
// docker run -d --name webserver nginx:1.27.0
// docker network inspect bridge
[
    {
        "Name": "bridge",
        "Id": "221c915074aaf7fd7c8a599d71b526dbcab1a1f949946e9e8a3d878b3ee338ed",
        "Created": "2026-08-15T01:17:36.95555023Z",
        "Scope": "local",
        "Driver": "bridge",     // <------------------------------
        "EnableIPv4": true,
        "EnableIPv6": false,
        "IPAM": {
            "Driver": "default",  // <------------------------------
            "Options": null,
            "Config": [
                {
                    "Subnet": "172.17.0.0/16",
                    "Gateway": "172.17.0.1"
                }
            ]
        },
        "Internal": false,
        "Attachable": false,
        "Ingress": false,
        "ConfigFrom": {
            "Network": ""
        },
        "ConfigOnly": false,
        "Options": {
            "com.docker.network.bridge.default_bridge": "true",
            "com.docker.network.bridge.enable_icc": "true",
            "com.docker.network.bridge.enable_ip_masquerade": "true",
            "com.docker.network.bridge.host_binding_ipv4": "0.0.0.0",
            "com.docker.network.bridge.name": "docker0",
            "com.docker.network.driver.mtu": "1500"
        },
        "Labels": {},
        "Containers": {
            "8ec0340a0c6a05a29af71f369059f65dfae98c4e5e30d490412dfb097e76002a": {
                "Name": "webserver",
                "EndpointID": "4f90313a66af7f4db75be1827e0869958ddc91fcc2e1ece430072a88bac57e14",
                "MacAddress": "6a:db:8c:78:17:e5",
                "IPv4Address": "172.17.0.2/16",   // <--------------------ip address
                "IPv6Address": ""
            }
        },
        "Status": {
            "IPAM": {
                "Subnets": {
                    "172.17.0.0/16": {
                        "IPsInUse": 4,
                        "DynamicIPsAvailable": 65532
                    }
                }
            }
        }
    }
]
```

The default bridge network is an older/basic compatibility mechanism.
**User-defined networks** provide better container-to-container networking, including:
- Automatic DNS resolution by container name
- Better network isolation
- Ability to create multiple independent networks
- Containers can be attached/detached dynamically

docker network inspect app-net  # User-defined networks provide better container-to-container networking


```bash
docker network create app-net # created a user-defined network
docker run -d --name webserver nginx:1.27.0  # without specifying network,default "bridge" is used. compared to the command below that use explicitly network  
                                             # docker run -d --network app-net --name webserver nginx:1.27.0 
docker network connect app-net webserver  # `docker network connect` connects an existing container (webserver) to a Docker network (app-net)
# user a linux container that joins the network to communicate with the webserver container using container name instead of  ip address
docker run -it --network app-net alpine:3.20 sh
# curl webserver
```
```json
// docker inspect webserver
[
    {
        "Id": "eed9822dff421234887084e8e16094bd2fe74c6d664efa55da1f10b1077d7e9e",
        "Created": "2026-08-15T14:24:08.19097204Z",
        "Path": "/docker-entrypoint.sh",
        "Args": [
            "nginx",
            "-g",
            "daemon off;"
        ],
        "State": {
            "Status": "running",
            "Running": true,
            "Paused": false,
            "Restarting": false,
            "OOMKilled": false,
            "Dead": false,
            "Pid": 13176,
            "ExitCode": 0,
            "Error": "",
            "StartedAt": "2026-08-15T14:24:08.272933731Z",
            "FinishedAt": "0001-01-01T00:00:00Z"
        },
        "Image": "sha256:900dca2a61f5799aabe662339a940cf444dfd39777648ca6a953f82b685997ed",
        "ResolvConfPath": "/var/lib/docker/containers/eed9822dff421234887084e8e16094bd2fe74c6d664efa55da1f10b1077d7e9e/resolv.conf",
        "HostnamePath": "/var/lib/docker/containers/eed9822dff421234887084e8e16094bd2fe74c6d664efa55da1f10b1077d7e9e/hostname",
        "HostsPath": "/var/lib/docker/containers/eed9822dff421234887084e8e16094bd2fe74c6d664efa55da1f10b1077d7e9e/hosts",
        "LogPath": "/var/lib/docker/containers/eed9822dff421234887084e8e16094bd2fe74c6d664efa55da1f10b1077d7e9e/eed9822dff421234887084e8e16094bd2fe74c6d664efa55da1f10b1077d7e9e-json.log",
        "Name": "/webserver",
        "RestartCount": 0,
        "Driver": "overlay2",
        "Platform": "linux",
        // ...
        },
        "Mounts": [],
        "Config": {
            "Hostname": "eed9822dff42",
            "Domainname": "",
            "User": "",
            "AttachStdin": false,
            "AttachStdout": false,
            "AttachStderr": false,
            "ExposedPorts": {
                "80/tcp": {}
            },
            "Tty": false,
            "OpenStdin": false,
            "StdinOnce": false,
            "Env": [
                "PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
                "NGINX_VERSION=1.27.0",
                "NJS_VERSION=0.8.4",
                "NJS_RELEASE=2~bookworm",
                "PKG_RELEASE=2~bookworm"
            ],
            "Cmd": [
                "nginx",
                "-g",
                "daemon off;"
            ],
            "Image": "nginx:1.27.0",
            "Volumes": null,
            "WorkingDir": "",
            "Entrypoint": [
                "/docker-entrypoint.sh"
            ],
            "Labels": {
                "desktop.docker.io/wsl-distro": "Ubuntu",
                "maintainer": "NGINX Docker Maintainers \u003cdocker-maint@nginx.com\u003e"
            },
            "StopSignal": "SIGQUIT"
        },
        "NetworkSettings": {
            "SandboxID": "ecbb3fe335981102090e5affd6cbdd01a776f436e134fc2a2fdb348e25ae58d9",
            "SandboxKey": "/var/run/docker/netns/ecbb3fe33598",
            "Ports": {
                "80/tcp": null
            },
            "Networks": {
                "app-net": {    // <-------------------------------------
                    "IPAMConfig": {},
                    "Links": null,
                    "Aliases": [],
                    "DriverOpts": {},
                    "GwPriority": 0,
                    "NetworkID": "b6856a3e5a3c1f59b3caa319ea5e7950efdf5387a66132b43273046e861bf21a",
                    "EndpointID": "c895f22f490f0ffcd45e4bd1458a95b275a1ba20aaafeaedca073d535d2d920d",
                    "Gateway": "172.19.0.1",
                    "IPAddress": "172.19.0.2",   // <-------------------------------------
                    "MacAddress": "d6:2b:f6:37:78:0c",
                    "IPPrefixLen": 16,
                    "IPv6Gateway": "",
                    "GlobalIPv6Address": "",
                    "GlobalIPv6PrefixLen": 0,
                    "DNSNames": [     // <-------------------------------------
                        "webserver",
                        "eed9822dff42"
                    ]
                },
                "bridge": {    // <-------------------------------------
                    "IPAMConfig": null,
                    "Links": null,
                    "Aliases": null,
                    "DriverOpts": null,
                    "GwPriority": 0,
                    "NetworkID": "221c915074aaf7fd7c8a599d71b526dbcab1a1f949946e9e8a3d878b3ee338ed",
                    "EndpointID": "f7fe15f43cea64a0902488478d335b744411fa3a46b9a78d5ac45605fa911e0c",
                    "Gateway": "172.17.0.1",
                    "IPAddress": "172.17.0.2",   // <-------------------------------------
                    "MacAddress": "0a:64:7f:14:a9:c0",
                    "IPPrefixLen": 16,
                    "IPv6Gateway": "",
                    "GlobalIPv6Address": "",
                    "GlobalIPv6PrefixLen": 0,
                    "DNSNames": null     // <--------------------------default network doesn't contain container name and id but app-net does
                }
            }
        }
    }
]
```
webserver container is now connnected to two networks `app-net` and `bridge` (when continer was created), note that its ip addrfess is different in those two networks


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


## `.override.yaml`

```yml
# compose.yaml
name: my-app

include:
  - a-notebooks-backend/compose.yaml
  - b-notes-backend/compose.yaml

services:
  reverse-proxy:
    image: nginx:1.27.0
    volumes:
      - type: bind
        source: ./c-reverse-proxy/nginx.conf
        target: /etc/nginx/nginx.conf
    ports:
      - 8080:80
    networks:
      - compose-net
    depends_on:
      - notebooks
      - notes

networks:
  compose-net:
```

```yml
# compose.override.yaml
services:
  notebooks:
    networks:
      - compose-net
  notes:
    networks:
      - compose-net
```
`docker compose up` will smartly load and use `compose.override.yaml` combined with `compose.yaml`


```
| Field type               | What override usually does         | Example                                              |
| ------------------------ | ---------------------------------- | ---------------------------------------------------- |
| **Scalar**               | **Override**                       | `image`, `command`, `restart`                        |
| **Mapping**              | **Merge keys; same keys override** | `environment`, `labels`, `deploy`                    |
| **Normal sequence/list** | **Append/merge**                   | `dns`, some list fields                              |
| **Unique resources**     | **Merge by unique key**            | `ports`, `volumes`, `secrets`, `configs`, `networks` |
| `!override`              | **Replace completely**             | Explicit replacement                                 |
| `!reset`                 | **Remove/reset**                   | Explicit removal                                     |
```
e.g for "networks", it is "merge/append" not override **despite it is called ".override.yaml"**:

```yml
# Base:
services:
  app:
    networks:
      - frontend

# .override.yaml:   
services:
  app:
    networks:
      - backend

# Final:
services:
  app:
    networks:
      - frontend
      - backend
```

note that if you add `!override` as:

```yml
# .override.yaml:   
services:
  app:
    networks: !override
      - backend

# Final:
services:
  app:
    networks:
      - backend  # now it is "override" not "merge", we normally don't do it for networks but for port mapping
```


## Others

**EXPOSE PORT**:

```Dockerfile
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
EXPOSE 3000  # <----------------------------------------------------

CMD [ "node", "index.js" ]
```

.dockerignore file like:
```yml
folderFoo
```
it will ignore folderFoo directories anywhere in the build context, including:
```
folderFoo/
src/folderFoo/
some-folder/folderFoo/
```
if you only wnat to specific folderFoo folder to exlude, use `/` (doesn't mean root directly, mean "root" relative in build contex) in prefix as : `/src/folderFoo/`
