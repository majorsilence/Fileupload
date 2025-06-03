# Fileupload

Small console app to test uploading files to sftp and azure blob servers.

# Options

## Select a provider, source, and destination

--provider <the type> --sourcepath "/path/to/local/file/to/upload/filename.ext" --destpath "/path/on/destination/server/filename.ext"

- Available provider types:  **sftp**, **azureblob**, **box**
- This tool defaults to uploading files.
- To switch to downloads set **--download**


## sftp options
  **--sftpusername** "<username>"
  **--sftppassword** "<password>"
  **--sftphost** "<host>"
  **--sftpport** <port>

## azureblob options
  **--azureconnectionstring** "<connectionstring>"
  --azurecontainer "<container>"

## box options
  - **--boxjsonconfigpath** "<filepath to json config>"
  - **--boxjsonconfigstring** "<ready to use json string instead of filepath>"
  - **--box-permit-file-update**
    - If a file exists with the same name in the destination folder then it will be overwritten


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

