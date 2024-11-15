﻿FROM mcr.microsoft.com/dotnet/aspnet:8.0.10-noble-amd64
#FROM mcr.microsoft.com/dotnet/runtime:8.0.10-noble-amd64
ENV ASPNET_VERSION=8.0.10

ARG BUILD_VERSION
ARG AUTHOR
ARG PROJECT_NAME
ARG PROJECT_OUTPUT

ENV RUN_AS_USER=1001
ENV RUN_AS_GROUP=1001
ENV RUN_AS_USERNAME=sparrow
ENV RUN_AS_GROUPNAME=sparrow

RUN groupadd -g $RUN_AS_GROUP $RUN_AS_USERNAME
RUN useradd -u $RUN_AS_USER -g $RUN_AS_GROUP $RUN_AS_USERNAME

# Create /app/entrypoint script, and make it executable
RUN mkdir -p /app
RUN echo '#!/bin/bash' > /app/entrypoint \
    && echo 'echo "entrypoint script"' >> /app/entrypoint \
    && echo 'dotnet /app/cert-task.dll "$1"' >> /app/entrypoint \
    && chmod +x /app/entrypoint

LABEL org.opencontainers.image.title=${PROJECT_NAME}
LABEL org.opencontainers.image.description="Docker image for ${PROJECT_NAME}"
LABEL org.opencontainers.image.version=${BUILD_VERSION}
LABEL org.opencontainers.image.authors=${AUTHOR}

COPY ${PROJECT_OUTPUT}/ app/
RUN chown -R $RUN_AS_USER:$RUN_AS_GROUP /app \
    && chmod -R 755 /app

USER $RUN_AS_USER:$RUN_AS_GROUP

RUN ls -la app/
WORKDIR /app
ENTRYPOINT ["/app/entrypoint"]