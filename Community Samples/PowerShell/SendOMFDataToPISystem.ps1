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

# ************************************************************************
# Import necessary packages
# ************************************************************************

# Note: this was tested against PowerShell version 5

# ************************************************************************
# Specify constant values (names, target URLS, etc.) needed by the script
# ************************************************************************

# Specify the name of this device, or simply use the hostname; this is the name
# of the PI AF Element that will be created, and it'll be included in the names
# of PI Points that get created as well
$DEVICE_NAME = $env:computername + "_POWERSHELL";
#$DEVICE_NAME = "MyCustomDeviceName"

# Specify a device location (optional); this will be added as a static
# attribute to the AF Element that is created
$DEVICE_LOCATION = "IoT Test Lab";

# Specify the name of the Assets type message; this will also end up becoming
# part of the name of the PI AF Element template that is created; for example, this could be
# "AssetsType_RaspberryPI" or "AssetsType_Dragonboard"
# You will want to make this different for each general class of IoT module that you use
$ASSETS_MESSAGE_TYPE_NAME = $DEVICE_NAME + "_assets_type" + "";
#$ASSETS_MESSAGE_TYPE_NAME = "assets_type" + "IoT Device Model 74656" # An example

# Similarly, specify the name of for the data values type; this should likewise be unique
# for each general class of IoT device--for example, if you were running this
# script on two different devices, each with different numbers and kinds of sensors,
# you'd specify a different data values message type name
# when running the script on each device.  If both devices were the same,
# you could use the same DATA_VALUES_MESSAGE_TYPE_NAME
$DATA_VALUES_MESSAGE_TYPE_NAME = "powershell_" + "data_values_type";
#$DATA_VALUES_MESSAGE_TYPE_NAME = "data_values_type" + "IoT Device Model 74656" # An example

# Store the id of the container that will be used to receive live data values
$DATA_VALUES_CONTAINER_ID = ($DEVICE_NAME + "_data_values_container");

# Specify the number of seconds to sleep in between value messages
$NUMBER_OF_SECONDS_BETWEEN_VALUE_MESSAGES = 2;

# Specify whether you're sending data to OSIsoft cloud services or not
$SEND_DATA_TO_OSISOFT_CLOUD_SERVICES = $false;

# Specify the address of the destination endpoint; it should be of the form
# http:#<host/ip>:<port>/ingress/messages
# For example, "https:#myservername:8118/ingress/messages"
$TARGET_URL = "https://lopezpiserver:777/ingress/messages";
# !!! Note: if sending data to OSIsoft cloud services,
# uncomment the below line in order to set the target URL to the OCS OMF endpoint:
#$TARGET_URL = "https://dat-a.osisoft.com/api/omf"
# !!! Note: if writing to an Edge Data Store, you can use the default tenant and namespace, so the URL would be
#$TARGET_URL = "https://localhost:5000/edge/omf/tenants/default/namespaces/data"

# Specify the producer token, a unique token used to identify and authorize a given OMF producer. Consult the OSIsoft Cloud Services or PI Connector Relay documentation for further information.
$PRODUCER_TOKEN = "OMFv1";
# !!! Note: if sending data to OSIsoft cloud services, the producer token should be the
# security token obtained for a particular Tenant and Publisher; see
# http:#qi-docs.readthedocs.io/en/latest/OMF_Ingress_Specification.html#headers
# !!! Note: if writing to an Edge Data Store, that producer token can be found here:
# https://osisoft-edge.readthedocs.io/en/latest/EdgeSecurity.html

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
# up with the values defined in the "DataValuesType" OMF message type (see the next section)
# In this example, this function simply generates two random values for the sensor values,
# but here is where you could change this function to reference a library that actually
# reads from sensors attached to the device that's running the script
function create_data_values_message()
{
    try { 
        # Get the current timestamp in ISO format
        $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        # Assemble a JSON object containing the streamId and any data values
        return ("[" +
            "{" +
                "`"containerid`": `"" + $DATA_VALUES_CONTAINER_ID + "`"," +
                "`"values`": [" +
                    "{" +
                        "`"Time`": `"" + $timestamp + "`"," +
                        # In this example,
                        # we're just sending along random values for these two `"sensors`"
                        "`"Raw Sensor Reading 1`":" + 0.1 * (Get-Random -Minimum 0 -Maximum 1000) + "," +
                        "`"Raw Sensor Reading 2`":" + 0.1 * (Get-Random -Minimum 0 -Maximum 1000) + "" +
                    "}" +
                "]" +
            "}" +
        "]");
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
        $response = Invoke-WebRequest -Uri $TARGET_URL -Headers $customHeaders -TimeoutSec $WEB_REQUEST_TIMEOUT_SECONDS -Body $message_json -Method POST -ContentType 'application/json'

        # Show the response
        write-host ("Response code: " + $response.StatusCode);
    }
    catch
    {
        # Log any error, if it occurs
        write-host ("" + (Get-Date) + " Error during web request: " + $_.Exception.Message);
    }
}



write-host (
	"`n--- Setup: targeting endpoint `"" + $TARGET_URL + "`"..." +
	"`n--- Now sending types, defining containers, and creating assets and links..." +
	"`n--- (Note: a successful message will return a 20X response code.)`n"
);

# ************************************************************************
# Create a JSON packet to define the types of streams that will be sent
# ************************************************************************

$DYNAMIC_TYPES_MESSAGE_JSON = "[" +

    # ************************************************************************
    # There are several different message types that will be used by this script, but
    # you can customize this script for your own needs by modifying the types:
    # First, you can modify the `"AssetsType`", which will allow you to customize which static
    # attributes are added to the new PI AF Element that will be created, and second," + 
    # you can modify the `"DataValuesType`", which will allow you to customize this script to send
    # additional sensor values, in addition to (or instead of) the two shown here

    # This values type is going to be used to send real-time values; feel free to rename the
    # values from `"Raw Sensor Reading 1`" to, say, `"Temperature`", or `"Pressure`"
    # Note:
    # all keywords (`"id`", `"type`", `"classification`", etc. are case sensitive!);
    # For a list of the specific keywords used in these messages," + 
    # see http:#omf-docs.readthedocs.io/

    "{" +
        "`"id`": `"" + $DATA_VALUES_MESSAGE_TYPE_NAME + "`"," +
        "`"type`": `"object`"," +
        "`"classification`": `"dynamic`"," +
        "`"properties`": {" +
            "`"Time`": {" +
            "`"format`": `"date-time`"," +
                "`"type`": `"string`"," +
                "`"isindex`": true" +
            "}," +
            "`"Raw Sensor Reading 1`": {" +
                "`"type`": `"number`"" +
            "}," +
            "`"Raw Sensor Reading 2`": {" +
                "`"type`": `"number`"" +
            "}" +
        # For example, to allow you to send a string-type live data value," + 
        # such as `"Status`", you would add
        #`"Status`": {
        #   `"type`": `"string`"
        #}
        "}" +
    "}" +
"]";

# ************************************************************************
# Send the DYNAMIC types message, so that these types can be referenced in all later messages
# ************************************************************************

send_omf_message_to_endpoint -action "create" -message_type "Type" -message_json $DYNAMIC_TYPES_MESSAGE_JSON
		
# !!! Note: if sending data to OCS, static types are not included!
if ($SEND_DATA_TO_OSISOFT_CLOUD_SERVICES -eq $false) {
	$STATIC_TYPES_MESSAGE_JSON = "[" +
        # This asset type is used to define a PI AF Element that will be created;
        # this type also defines two static string attributes that will be created
        # as well; feel free to rename these or add additional
        # static attributes for each Element (PI Point attributes will be added later);
        # The name of this type will also end up being part of the name of the PI AF Element template
        # that is automatically created
        "{" +
            "`"id`": `"" + $ASSETS_MESSAGE_TYPE_NAME + "`"," +
            "`"type`": `"object`"," +
            "`"classification`": `"static`"," +
            "`"properties`": {" +
                "`"Name`": {" +
                    "`"type`": `"string`"," +
                    "`"isindex`": true" +
                "}," +
                "`"Device Type`": {" +
                    "`"type`": `"string`"" +
                "}," +
                "`"Location`": {" +
                    "`"type`": `"string`"" +
                "}," +
                "`"Data Ingress Method`": {" +
                    "`"type`": `"string`"" +
                "}" +
            # For example, to add a number-type static
            # attribute for the device model, you would add
            # `"Model`": {
            #   `"type`": `"number`"
            #}
            "}" +
        "}" +
    "]";

    # ************************************************************************
    # Send the STATIC types message, so that these types can be referenced in all later messages
    # ************************************************************************

    send_omf_message_to_endpoint -action "create" -message_type "Type" -message_json $STATIC_TYPES_MESSAGE_JSON
}

# ************************************************************************
# Create a JSON packet to define container IDs and the type
# (using the types listed above) for each new data events container
# ************************************************************************

# The device name that you specified earlier will be used as the AF Element name!
$NEW_AF_ELEMENT_NAME = $DEVICE_NAME;

$CONTAINERS_MESSAGE_JSON = "[" +
    "{" +
        "`"id`": `"" + $DATA_VALUES_CONTAINER_ID + "`"," +
        "`"typeid`": `"" + $DATA_VALUES_MESSAGE_TYPE_NAME + "`"" +
    "}" +
"]";

# ************************************************************************
# Send the container message, to instantiate this particular container;
# we can now directly start sending data to it using its Id
# ************************************************************************

send_omf_message_to_endpoint -action "create" -message_type "Container" -message_json $CONTAINERS_MESSAGE_JSON;

# !!! Note: if sending data to OCS, assets and links are not included!
if ($SEND_DATA_TO_OSISOFT_CLOUD_SERVICES -eq $false) {
		
	# ************************************************************************
	# Create a JSON packet to containing the asset and
	# linking data for the PI AF asset that will be made
	# ************************************************************************

	# Here is where you can specify values for the static PI AF attributes;
	# in this case, we"re auto-populating the Device Type," + 
	# but you can manually hard-code in values if you wish
	# we also add the LINKS to be made, which will both position the new PI AF
	# Element, so it will show up in AF, and will associate the PI Points
	# that will be created with that Element
	$ASSETS_AND_LINKS_MESSAGE_JSON = "[" +
        "{" +
            # This will end up creating a new PI AF Element with
            # this specific name and static attribute values
            "`"typeid`": `"" + $ASSETS_MESSAGE_TYPE_NAME + "`"," +
            "`"values`": [" +
                "{" +
                    "`"Name`":`"" + $NEW_AF_ELEMENT_NAME + "`"," +
                    "`"Device Type`": `"" + "Type74656" + "`"," +
                    "`"Location`": `"" + $DEVICE_LOCATION + "`"," +
                    "`"Data Ingress Method`": `"OMF`"" +
                "}" +
            "]" +
        "}," +
        "{" +
            "`"typeid`": `"__Link`"," +
            "`"values`": [" +
                # This first link will locate such a newly created AF Element under
                # the root PI element targeted by the PI Connector in your target AF database
                # This was specified in the Connector Relay Admin page; note that a new
                # parent element, with the same name as the PRODUCER_TOKEN, will also be made
                "{" +
                    "`"Source`": {" +
                        "`"typeid`": `"" + $ASSETS_MESSAGE_TYPE_NAME + "`"," +
                        "`"index`": `"_ROOT`"" +
                    "}," +
                    "`"Target`": {" +
                        "`"typeid`": `"" + $ASSETS_MESSAGE_TYPE_NAME + "`"," +
                        "`"index`": `"" + $NEW_AF_ELEMENT_NAME + "`"" +
                    "}" +
                "}," +
                # This second link will map new PI Points (created by messages
                # sent to the data values container) to a newly create element
                "{" +
                    "`"Source`": {" +
                        "`"typeid`": `"" + $ASSETS_MESSAGE_TYPE_NAME + "`"," +
                        "`"index`":`"" + $NEW_AF_ELEMENT_NAME + "`"" +
                    "}," +
                    "`"Target`": {" +
                        "`"containerid`": `"" + $DATA_VALUES_CONTAINER_ID + "`"" +
                    "}" +
                "}" +
            "]" +
        "}" +
    "]";

    # ************************************************************************
    # Send the message to create the PI AF asset; it won"t appear in PI AF," + 
    # though, because it hasn't yet been positioned...
    # ************************************************************************

    send_omf_message_to_endpoint -action "create" -message_type "Data" -message_json $ASSETS_AND_LINKS_MESSAGE_JSON

}

# ************************************************************************
# Initialize sensors prior to sending data (if needed), using the function defined earlier
# ************************************************************************

initialize_sensors;

# ************************************************************************
# Finally, loop indefinitely, sending random events
# conforming to the value type that we defined earlier
# ************************************************************************

write-host (
	"`n--- Now sending live data every " + ($NUMBER_OF_SECONDS_BETWEEN_VALUE_MESSAGES) +
	" second(s) for device `"" + $NEW_AF_ELEMENT_NAME + "`"... (press CTRL+C to quit at any time)`n"
);
if ($SEND_DATA_TO_OSISOFT_CLOUD_SERVICES -eq $false) {
	write-host (
		"--- (Look for a new AF Element named `"" + $NEW_AF_ELEMENT_NAME + "`".)`n"
	);
}
while ($true) {
	# Call the custom function that builds a JSON object that
	# contains new data values; see the beginning of this script
	$VALUES_MESSAGE_JSON = create_data_values_message;

    # Send the JSON message to the target URL
    send_omf_message_to_endpoint -action "create" -message_type "Data" -message_json $VALUES_MESSAGE_JSON

	# Send the next message after the required interval
	Start-Sleep -Seconds $NUMBER_OF_SECONDS_BETWEEN_VALUE_MESSAGES
}

