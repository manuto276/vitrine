FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive
ENV TZ=Europe/Rome

RUN apt-get update && apt-get install -y \
    curl wget git git-flow tree unzip zip nano vim \
    ca-certificates gnupg lsb-release \
    build-essential \
    libc6 libgcc-s1 libgssapi-krb5-2 \
    libicu74 liblttng-ust1 libssl3 \
    libstdc++6 zlib1g \
    procps htop sudo \
    && rm -rf /var/lib/apt/lists/*

RUN curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/local/dotnet \
    && rm /tmp/dotnet-install.sh

RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Wine + Inno Setup (for building installer on Linux)
RUN dpkg --add-architecture i386 \
    && apt-get update \
    && apt-get install -y --no-install-recommends wine wine64 xvfb \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /opt/innosetup \
    && cd /tmp \
    && wget -q "https://files.jrsoftware.org/is/6/innosetup-6.4.3.exe" -O is-setup.exe \
    && xvfb-run wine is-setup.exe /VERYSILENT /SUPPRESSMSGBOXES /DIR="C:\\InnoSetup" \
    && cp -r /root/.wine/drive_c/InnoSetup/* /opt/innosetup/ \
    && rm -f is-setup.exe \
    && rm -rf /tmp/.wine*

ENV DOTNET_ROOT=/usr/local/dotnet
ENV PATH="$PATH:/usr/local/dotnet"
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV NUGET_PACKAGES=/workspace/.nuget/packages

RUN echo "ubuntu ALL=(root) NOPASSWD:ALL" > /etc/sudoers.d/ubuntu \
    && chmod 0440 /etc/sudoers.d/ubuntu

USER ubuntu

RUN dotnet tool install --global dotnet-outdated-tool \
    && dotnet tool install --global dotnet-format \
    && dotnet tool install --global csharpier

ENV PATH="$PATH:/home/ubuntu/.dotnet/tools"

WORKDIR /workspace