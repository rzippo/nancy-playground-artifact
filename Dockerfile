FROM debian:bookworm

RUN apt-get update && \
    apt-get install -y sudo wget curl libicu72 git \
    openjdk-17-jdk \
    # To run Chromium and Firefox
    libxkbcommon0 libgbm1 libgtk-3-0

# Create the non-root user, with sudo access
ARG USER_UID=1000
ARG USER_GID=$USER_UID
RUN groupadd --gid $USER_GID dotnet \
    && useradd --uid $USER_UID --gid $USER_GID -m dotnet \
    && echo dotnet ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/dotnet \
    && chmod 0440 /etc/sudoers.d/dotnet

# Add powershell 7.6.1, using deb package
USER root
RUN wget https://github.com/PowerShell/PowerShell/releases/download/v7.6.1/powershell_7.6.1-1.deb_amd64.deb
RUN dpkg -i powershell_7.6.1-1.deb_amd64.deb
## Cleanup
RUN rm powershell_7.6.1-1.deb_amd64.deb

# Add dotnet 8.0, 9.0 and 10.0, using user-local install
USER dotnet
RUN wget https://dot.net/v1/dotnet-install.sh -O /home/dotnet/dotnet-install.sh && \
    chmod +x /home/dotnet/dotnet-install.sh
RUN /home/dotnet/dotnet-install.sh --channel 8.0
RUN /home/dotnet/dotnet-install.sh --channel 9.0
RUN /home/dotnet/dotnet-install.sh --channel 10.0
## Cleanup
RUN rm /home/dotnet/dotnet-install.sh

ENV PATH="/home/dotnet/.dotnet:${PATH}"
ENV DOTNET_ROOT="/home/dotnet/.dotnet"

# Install dontnet tools for test coverage and report generation
ENV PATH="/home/dotnet/.dotnet/tools:${PATH}"
RUN dotnet tool install --global coverlet.console --version 10.0.0
RUN dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.5.6

# Add ANTLR command line tools
USER root
RUN wget https://www.antlr.org/download/antlr-4.13.2-complete.jar -O /usr/local/lib/antlr-4.13.2-complete.jar

# This assumes CLASSPATH is currently empty
ENV CLASSPATH=".:/usr/local/lib/antlr-4.13.2-complete.jar"

# Set the default user as non-root
USER dotnet
