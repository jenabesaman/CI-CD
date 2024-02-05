# Use the official .NET 6 image from the Docker Hub
FROM mcr.microsoft.com/dotnet/aspnet:6.0

# Copy the files from /home/DSTV3.UploadInterface.Api/ to /home/DSTV3.UploadInterface.Api/ in the Docker container
COPY /home/DSTV3.UploadInterface.Api/ /home/DSTV3.UploadInterface.Api/

# Set the working directory
WORKDIR /home/test2/dstv3.uploadinterface.Api/

# Expose port 8503
EXPOSE 8503

# Start the app
ENTRYPOINT ["dotnet", "DSTV3.UploadInterface.Api.dll"]