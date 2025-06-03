# Fileupload

Small console app to test uploading files to sftp and azure blob servers.


# Examples

box.com upload example

```bash
fileupload --provider box \
    --boxjsonconfigpath "/path/to/box_com_api_jwt_config.json" \
    --sourcepath "/path/to/source/file" \
    --destpath "/box/path/to/upload/file" \
    --box-permit-file-update
```

box.com download example

```bash
fileupload --provider box \
    --boxjsonconfigpath "/path/to/box_com_api_jwt_config.json" \
    --download \
    --sourcepath "/box/path/to/download/file/from"
    --destpath "/local/path/to/save/file"
```

