FROM alpine:3.20

# the order of CMD and ENTRYPOINT does not matter
ENTRYPOINT ["echo"]
CMD ["DEFAULT MESSAGE"]

# execve(
#     "/bin/echo",
#     {
#         "echo",
#         "DEFAULT MESSAGE",
#         NULL
#     },
#     envp
# );