#Copyright 2018 OSIsoft, LLC
#
#Licensed under the Apache License, Version 2.0 (the "License");
#you may not use this file except in compliance with the License.
#You may obtain a copy of the License at
#
#<http:#www.apache.org/licenses/LICENSE-2.0>
#
#Unless required by applicable law or agreed to in writing, software
#distributed under the License is distributed on an "AS IS" BASIS,
#WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
#See the License for the specific language governing permissions and
#limitations under the License.

# NOTE: this script was designed using the v1.0
# version of the OMF specification, as outlined here:
# http:#omf-docs.readthedocs.io/en/v1.0/index.html

# Note: this was tested against PowerShell version 5

# Overview
# Read files formatted using the OSIsoft Message Format Specification and send to the configured endpoint
#
# Usaage
# Update the variables in the following section to reflect your environment
# Optionally update the files in the data folder to send your own OMF messages
# Run the script!
#
#  Requirements
# 1. PowerShell
# 2. OSIsoft Message Format endpoint - i.e.: Edge Data Store (EDS), PI Connector Relay (Relayr) or OSIsoft CLoud Services
# 3. Producer Token obtained from the endpoint
#4. For EDS and Relay, the endpoint URL
#5. (optional) customized OMF messages based on their type in the following files, located in the directory identified by the $OMF_FILES_PATH variable
#      type .json
#      container.json
#      data-asset.json
#      data-link.json

# ************************************************************************
# Specify constant values (names, target URLS, etc.) needed by the script
# ************************************************************************

# Specify the address of the destination endpoint; it should be of the form
# http:#<host/ip>:<port>/ingress/messages
# For example, "https:#myservername:8118/ingress/messagess"
$TARGET_URL = "https://myserver.mydomain:8118/ingress/messages";

# !!! Note: if sending data to OSIsoft cloud services,
# uncomment the below line in order to set the target URL to the OCS OMF endpoint:
#$TARGET_URL = "https://dat-a.osisoft.com/api/omf"

# Specify the producer token, a unique token used to identify and authorize a given OMF producer. Consult the OSIsoft Cloud Services or PI Connector Relay documentation for further information.
# !!! Note: if sending data to OSIsoft cloud services, the producer token should be the
# security token obtained for a particular Tenant and Publisher; see
# http:#qi-docs.readthedocs.io/en/latest/OMF_Ingress_Specification.html#headers
$PRODUCER_TOKEN = "OMFv1";

# Specify whether you're sending data to OSIsoft cloud services or not
$SEND_DATA_TO_OSISOFT_CLOUD_SERVICES = $FALSE;

# Identify where the type, container and data files are located that will be loaded and sent using this script
$OMF_FILES_PATH=".\data";

# Specify the number of seconds to sleep in between value messages
$NUMBER_OF_SECONDS_BETWEEN_VALUE_MESSAGES = 2;

# Specify whether you're sending data to OSIsoft cloud services or not
$SEND_DATA_TO_OSISOFT_CLOUD_SERVICES = $FALSE;

# !!! Note: if sending data to OSIsoft cloud services, the producer token should be the
# security token obtained for a particular Tenant and Publisher; see
# http:#qi-docs.readthedocs.io/en/latest/OMF_Ingress_Specification.html#headers

# ************************************************************************
# Specify options for sending web requests to the target
# ************************************************************************

# To enforce certificate valdiation, comment out the below lines
#<#
add-type -TypeDefinition  @"
        using System.Net;
        using System.Security.Cryptography.X509Certificates;
        public class TrustAllCertsPolicy : ICertificatePolicy {
            public bool CheckValidationResult(
                ServicePoint srvPoint, X509Certificate certificate,
                WebRequest request, int certificateProblem) {
                return true;
            }
        }
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
##>

# Specify the timeout, in seconds, for sending web requests
# (if it takes longer than this to send a message, an error will be thrown)
$WEB_REQUEST_TIMEOUT_SECONDS = 30

# ************************************************************************
# Helper function: run any code needed to initialize local sensors, if necessary for this hardware
# ************************************************************************

# Below is where you can initialize any global variables that are needed by your application;
# certain sensors, for example, will require global interface or sensor variables
# myExampleInterfaceKitGlobalVar = None

# The following function is where you can insert specific initialization code to set up
# sensors for a particular IoT module or platform
function initialize_sensors()
{
    write-host ("`n--- Sensors initializing...");
    try
    {
        write-host ("--- Sensors initialized!");
        # In short, in this example, by default,
        # this function is called but doesn't do anything (it's just a placeholder)
    }
    catch
    {
        # Log any error, if it occurs
        write-host ("" + (Get-Date) + " Error when initializing sensors: " + $_.Exception.Message);
    }
}

# ************************************************************************
# Helper function: REQUIRED: create a JSON message that contains sensor data values
# ************************************************************************

# The following function you can customize to allow this script to send along any
# number of different data values, so long as the values that you send here match
# up with the values defined in files
function create_data_values_message()
{
    try { 
        # Get the current timestamp in ISO format
        $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
		
		# retrieve the data
		$CONTAINERS_MESSAGE_JSON = Get-Content  $OMF_FILES_PATH\data.json -raw;
		# replate the variable with the current time
		$CONTAINERS_MESSAGE_JSON  = $CONTAINERS_MESSAGE_JSON  -replace "TIMESTAMP_NOW",$timestamp;
		
        # Assemble a JSON object containing the streamId and any data values
        return ($CONTAINERS_MESSAGE_JSON);
    }
    catch
    {
        # Log any error, if it occurs
        write-host ("" + (Get-Date) + " Error creating data message: " + $_.Exception.Message);
        return "{}";
    }
}

# ************************************************************************
# Helper function: REQUIRED: wrapper function for sending an HTTPS message
# ************************************************************************

# Define a helper function to allow easily sending web request messages;
# this function can later be customized to allow you to port this script to other languages.
# All it does is take in a data object and a message type, and it sends an HTTPS
# request to the target OMF endpoint
function send_omf_message_to_endpoint
{
    param([string]$action = "",
            [String]$message_type = "",
            [String]$message_json = "")
    try
    {
        # Assemble headers that contain the producer token and message type
        # Note: in this example, the only action that is used is `"create`",
        # which will work totally fine;
        # to expand this application, you could modify it to use the `"update`"
        # action to, for example, modify existing AF element template types
        $customHeaders = New-Object "System.Collections.Generic.Dictionary[[String],[String]]" 
        $customHeaders.Add('producertoken', $PRODUCER_TOKEN)
        $customHeaders.Add('messagetype', $message_type)
        $customHeaders.Add('action', $action)
        $customHeaders.Add('messageformat', 'JSON')
        $customHeaders.Add('omfversion', '1.0')

        # !!! Note: if desired, uncomment the below line to write-host  the outgoing message
        write-host ("`nOutgoing message: " + $message_json);
        # Send the request, and collect the response
		$response = $null
        $response = Invoke-WebRequest -Uri $TARGET_URL -Headers $customHeaders -TimeoutSec $WEB_REQUEST_TIMEOUT_SECONDS -Body $message_json -Method POST -ContentType 'application/json'

        # Show the responses
        write-host ("Response code: " + $response.StatusCode);
   }
    catch
    {
        # Log any error, if it occurs
       write-host ("" + (Get-Date) + " Error during web request: " + $_.Exception.Message);
	   # Output any message content from the Server
	   if (${$_ .ErrorDetails.Message} -eq $null) {
          $_.ErrorDetails.Message
       }
    }
}

write-host (
	"`n--- Setup: targeting endpoint `"" + $TARGET_URL + "`"..." +
	"`n--- Now sending types, defining containers, and creating assets and links..." +
	"`n--- (Note: a successful message will return a 20X response code.)`n"
);

write-host "Sending Types";
$TYPES_MESSAGE_JSON = Get-Content  $OMF_FILES_PATH\type.json -raw
send_omf_message_to_endpoint -action "create" -message_type "Type" -message_json $TYPES_MESSAGE_JSON

write-host "Sending Containers";
$CONTAINERS_MESSAGE_JSON = Get-Content  $OMF_FILES_PATH\container.json -raw
send_omf_message_to_endpoint -action "create" -message_type "Container" -message_json $CONTAINERS_MESSAGE_JSON;

#
# OSIsoft Cloud services does nto currently support assets or links, so don't send.
#
if ( ! $SEND_DATA_TO_OSISOFT_CLOUD_SERVICES)  { 

		write-host "Sending Assets";
		$DATA_ASSETS_JSON = Get-Content  $OMF_FILES_PATH\data-asset.json -raw
		send_omf_message_to_endpoint -action "create" -message_type "Data" -message_json $DATA_ASSETS_JSON

		write-host "Sending Links";
		$LINKS_ASSETS_JSON = Get-Content  $OMF_FILES_PATH\data-link.json -raw
		send_omf_message_to_endpoint -action "create" -message_type "Data" -message_json $LINKS_ASSETS_JSON
}

write-host (
	"`n--- Now sending live data every " + ($NUMBER_OF_SECONDS_BETWEEN_VALUE_MESSAGES) +
	" second(s) ... (press CTRL+C to quit at any time)`n"
);
while ($TRUE) {
	# Call the custom function that builds a JSON object that
	# contains new data values; see the beginning of this script
	$VALUES_MESSAGE_JSON = create_data_values_message;
	
    # Send the JSON message to the target URL
    send_omf_message_to_endpoint -action "create" -message_type "Data" -message_json $VALUES_MESSAGE_JSON

	# Send the next message after the required interval
	Start-Sleep -Seconds $NUMBER_OF_SECONDS_BETWEEN_VALUE_MESSAGES
}