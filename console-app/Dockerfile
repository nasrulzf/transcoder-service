# Use the official .NET runtime image as a base
FROM mcr.microsoft.com/dotnet/runtime:8.0

# Install FFmpeg
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    rm -rf /var/lib/apt/lists/*

# Set the working directory
WORKDIR /app
