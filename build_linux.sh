#!/bin/sh

VERSION=$(cat FileUpload/FileUpload.csproj | grep "<Version>" | sed 's/[^0-9.]*//g')

dotnet restore FileUpload.sln
dotnet build -c Release FileUpload.sln --no-restore
dotnet publish -c Release -r linux-x64 --self-contained true --output ./build/output/linux-x64
dotnet publish -c Release -r linux-arm64 --self-contained true --output ./build/output/linux-arm64

rm -rf build/linux
mkdir -p build/linux/opt/majorsilence/fileupload
cp -r build/output/linux-x64/* build/linux/opt/majorsilence/fileupload/
chmod +x build/linux/opt/majorsilence/fileupload/FileUpload
mkdir -p build/linux/usr/bin
cp -r majorsilence-fileupload build/linux/usr/bin/majorsilence-fileupload
chmod +x build/linux/usr/bin/majorsilence-fileupload


sudo gem install fpm --no-document

cd build/linux
# build a deb package using fpm
fpm -s dir -t deb \
  --name majorsilence-fileupload \
  --version $VERSION \
  --description "Majorsilence Fileupload tool.  Interact with azure blob storage, box.com and others as if they are ftp servers." \
  --maintainer "Majorsilence" \
  --license "MIT" \
  --architecture all \
  --deb-no-default-config-files \
  --url "https://github.com/majorsilence/Fileupload" \
  --maintainer "Peter Gill <peter@majorsilence.com>" \
  ./

  cd ../../