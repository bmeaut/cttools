# FastAPI interface for python operations

Extending cv4s operations with python operations. 
There can be cases when adding an operation or making a demo/research project is much easier in python 
than in the main C# framework. This lightweight webserver is providing an interface where there is no restrictions 
on package or python interpreter versions. 

## Development state
The project works as a demo for future operations.
Logic for working with the BlobImages will need to be added.

## Developing new endpoints
Adding functionality is intended to be done with adding new endpoints and developing a simple Operation in cv4s 
that will call the proper endpoint. However if there is any package version conflict with other operations, 
a whole new webserver can be configured using the current code as a template.

## Production
If the server and python operations will be used on production builds, installing and starting the webserver 
should be done as part of the installation process.
Please use requirements.txt during development (or other package tracking tool) to track the packages needed.

## Docs
The endpoint and schema docs are available on 127.0.0.1:8000/docs.
> FastAPI creates the OpenAPI docs automatically. 

## Start the server
Call one of
- bin/start_server.sh
- python start_server.py

## Future development tasks
- serialize and pass blobImage instead of raw image
- process different return values
  - internalOutput diagram
  - tag values to add
  - tag values to remove
  - changed blobImages
- tools for python BlobImage processing
- optimize serialization time
- add logging
