FROM  nginx:1.27.0

RUN apt-get update 
RUN apt-get -y install vim

COPY index.html /usr/share/nginx/html/index.html 

# each instruction runs as the current Dockerfile user which is root user in root group

# root (the owner of index.html inside the container will be root:root)
#  |
#  +-- nginx master process (the nginx master process runs as the existing root user)
#         |
#         +-- nginx worker process1 (user: nginx,  group: nginx)
#         +-- nginx worker process2 (user: nginx,  group: nginx)
#         ...  
# you will need below command to change the ownership of the index.html file (for read is fine, for write you need to change ownership) to nginx user and group,
# otherwise you will get permission denied error when you try to access the index.html file from browser

# RUN chown nginx:nginx /usr/share/nginx/html/index.html

# Where does the name root come from?
# The Linux filesystem has a user database: /etc/passwd which contains information about users, including the root user
