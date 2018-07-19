
Purpose:
Read files formatted using the OSIsoft Message Format Specification and send to the configured endpoint.
Useful to learn and/or debug OMF messages.

Requirement: 
1. PowerShell
2. OSIsoft Message Format endpoint - i.e.: Edge Data Store (EDS), PI Connector Relay (Relayr) or OSIsoft CLoud Services
3. Producer Token obtained from the endpoint
4. For EDS and Relay, the endpoint URL
5. (optional) customized OMF messages based on their type in the following files, located in the directory identified by the $OMF_FILES_PATH variable
      type .json
      container.json
      data-asset.json
      data-link.json

Setup
1. Update the variables in the first section of the PowerShell script
2. Optionally update the files in the data folder to send your own OMF messages
3.   Run the script from PowerShell:
      .\omfDemo.ps1

Files:
README.md
omfDemo.ps1
data\type.json  - specify the OMF types
data\container.json - specify the OMF containers
data\data-link.json - specify the OMF links
data\data-asset.json - specify the OMF assets
data\data.json - data values, note: TIMESTAMP_NOW is updated by the script when sending the data file.

See also:
http://omf-docs.readthedocs.io - OSIsoft Message format specification

Tested using PowerShell 5.1 on Windows 10