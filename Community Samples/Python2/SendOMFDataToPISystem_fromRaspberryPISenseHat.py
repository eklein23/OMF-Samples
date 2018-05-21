#Copyright 2018 OSIsoft, LLC
#
#Licensed under the Apache License, Version 2.0 (the "License");
#you may not use this file except in compliance with the License.
#You may obtain a copy of the License at
#
#<http://www.apache.org/licenses/LICENSE-2.0>
#
#Unless required by applicable law or agreed to in writing, software
#distributed under the License is distributed on an "AS IS" BASIS,
#WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
#See the License for the specific language governing permissions and
#limitations under the License.

# NOTE: this script was designed using the v1.0
# version of the OMF specification, as outlined here:
# http://omf-docs.readthedocs.io/en/v1.0/index.html

# ************************************************************************
# Import necessary packages
# ************************************************************************

# Import packages
import json
import time
import platform
import socket
import datetime
import random # Used to generate sample data; comment out this line if real data is used
import requests

# Import any special packages needed for a particular hardware platform,
# for example, for a Raspberry PI,
# import RPi.GPIO as GPIO
import sense_hat   # Used for controlling the Sense Hat
import sys         # Used to parse error messages
import os          # Used to sync the internal clock
import socket      # Used to get the current host name
import webcolors   # Used to allow easily referencing colors by name

# ************************************************************************
# Specify constant values (names, target URLS, et centera) needed by the script
# ************************************************************************

# Specify the name of this device, or simply use the hostname; this is the name
# of the PI AF Element that will be created, and it'll be included in the names
# of PI Points that get created as well
DEVICE_NAME = (socket.gethostname()) + " Raspberry Pi"
#DEVICE_NAME = "MyCustomDeviceName"

# Specify a device location (optional); this will be added as a static
# string attribute to the AF Element that is created
DEVICE_LOCATION = "IoT Test Lab"

# Specify the name of the Assets type message; this will also end up becoming
# part of the name of the PI AF Element template that is created; for example, this could be
# "AssetsType_RaspberryPI" or "AssetsType_Dragonboard"
# You will want to make this different for each general class of IoT module that you use
ASSETS_MESSAGE_TYPE_NAME = DEVICE_NAME + "_assets_type"
#ASSETS_MESSAGE_TYPE_NAME = "assets_type" + "IoT Device Model 74656" # An example
# NoteL you can repalce DEVICE_NAME with DEVICE_TYPE if you'd like to use a common type for multiple assets

# Similarly, specify the name of for the data values type; this should likewise be unique
# for each general class of IoT device--for example, if you were running this
# script on two different devices, each with different numbers and kinds of sensors,
# you'd specify a different data values message type name
# when running the script on each device.  If both devices were the same,
# you could use the same DATA_VALUES_MESSAGE_TYPE_NAME
DATA_VALUES_MESSAGE_TYPE_NAME = DEVICE_NAME + "_data_values_type"
#DATA_VALUES_MESSAGE_TYPE_NAME = "data_values_type" + "IoT Device Model 74656" # An example
# NoteL you can repalce DEVICE_NAME with DEVICE_TYPE if you'd like to use a common type for multiple assets

# Store the id of the container that will be used to receive live data values
DATA_VALUES_CONTAINER_ID = DEVICE_NAME + "_data_values_container"

# Specify the number of seconds to sleep in between value messages
NUMBER_OF_SECONDS_BETWEEN_VALUE_MESSAGES = 2

# Specify whether you're sending data to OSIsoft cloud services or not
SEND_DATA_TO_OSISOFT_CLOUD_SERVICES = False

# Specify the address of the destination endpoint; it should be of the form
# http://<host/ip>:<port>/ingress/messages
# For example, "https://myservername:8118/ingress/messages"
TARGET_URL = "https://pisystem.cloudapp.azure.com:5460/ingress/messages"
# !!! Note: if sending data to OSIsoft cloud services,
# uncomment the below line in order to set the target URL to the OCS OMF endpoint:
#TARGET_URL = "https://dat-a.osisoft.com/api/omf"

# Specify the producer token, a unique token used to identify and authorize a given OMF producer. Consult the OSIsoft Cloud Services or PI Connector Relay documentation for further information.
PRODUCER_TOKEN = "OMFv1"
#PRODUCER_TOKEN = "778408" # An example
# !!! Note: if sending data to OSIsoft cloud services, the producer token should be the
# security token obtained for a particular Tenant and Publisher; see
# http://qi-docs.readthedocs.io/en/latest/OMF_Ingress_Specification.html#headers
#PRODUCER_TOKEN = ""

# ************************************************************************
# Specify options for sending web requests to the target
# ************************************************************************

# If self-signed certificates are used (true by default),
# do not verify HTTPS SSL certificates; normally, leave this as is
VERIFY_SSL = False

# Specify the timeout, in seconds, for sending web requests
# (if it takes longer than this to send a message, an error will be thrown)
WEB_REQUEST_TIMEOUT_SECONDS = 30

# ************************************************************************
# Helper function: run any code needed to initialize local sensors, if necessary for this hardware
# ************************************************************************

# Below is where you can initialize any global variables that are needed by your applicatio;
# certain sensors, for example, will require global interface or sensor variables
# myExampleInterfaceKitGlobalVar = None

# Specify whether the lights should turn off at night;
# if set to true, LEDs will be disabled between 10 PM - 7 AM
NIGHT_MODE_ENABLED = True

# Define the color bar that will be used for each row of LEDs
RED_TO_GREEN_COLOR_BAR = [
    webcolors.name_to_rgb('MAGENTA'),
    webcolors.name_to_rgb('RED'),
    webcolors.name_to_rgb('ORANGE'),
    webcolors.name_to_rgb('YELLOW'),
    webcolors.name_to_rgb('YELLOWGREEN'),
    webcolors.name_to_rgb('GREEN'),
    webcolors.name_to_rgb('LightSeaGreen'),
    webcolors.name_to_rgb('BLUE')
]
# Specify a default background color for LEDs
DEFAULT_BACKGROUND_COLOR = webcolors.name_to_rgb('navy')

# Initialize a global array for holding the most recent 8 readings
recentReadings = [1, 1, 1, 1, 1, 1, 1, 1]

# Initialize the sensor hat object
sense = sense_hat.SenseHat()

# The following function is where you can insert specific initialization code to set up
# sensors for a particular IoT module or platform
def initialize_sensors():
    print("\n--- Sensors initializing...")
    try:
		#For a raspberry pi, for example, to set up pins 4 and 5, you would add
        #GPIO.setmode(GPIO.BCM)
		#GPIO.setup(4, GPIO.IN)
		#GPIO.setup(5, GPIO.IN)
        print("--- Waiting 10 seconds for sensors to warm up...")
        time.sleep(10)

        # Activate the compass, gyro, and accelerometer
        sense.set_imu_config(True, True, True)
        sense.show_message("Ready!")
        print("Gyro initialized...")

        print("--- Sensors initialized!")
		
        # Sync the time on this device to an internet time server
        try:
            print('Syncing time...')
            os.system('sudo service ntpd stop')
            time.sleep(1)
            os.system('sudo ntpd -gq')
            time.sleep(1)
            os.system('sudo service ntpd start')
            print('Success! Time is ' + str(datetime.datetime.now()))
        except:
            print('Error syncing time!')

    except Exception as ex:
		# Log any error, if it occurs
        print(str(datetime.datetime.now()) + " Error when initializing sensors: " + str(ex))

# ************************************************************************
# Helper function: REQUIRED: create a JSON message that contains sensor data values
# ************************************************************************

# The following function you can customize to allow this script to send along any
# number of different data values, so long as the values that you send here match
# up with the values defined in the "DataValuesType" OMF message type (see the next section)
# In this example, this function simply generates two random values for the sensor values,
# but here is where you could change this function to reference a library that actually
# reads from sensors attached to the device that's running the script

def create_data_values_message():
    # Get the current timestamp in ISO format
    timestamp = datetime.datetime.utcnow().isoformat() + 'Z'
    # Read sensors
    pitch, roll, yaw = sense.get_orientation_degrees().values()
    degreesToNorth = sense.get_compass()
    accelerationx, accelerationy, accelerationz = sense.get_accelerometer_raw().values()
    # Update the sense hat display!
    update_sense_hat_display(accelerationz)
    # Assemble a JSON object containing the streamId and any data values
    return [
        {
            "containerid": DATA_VALUES_CONTAINER_ID,
            "values": [
                {
                    "Time": timestamp,
                    # Again, in this example,
                    # we're just sending along random values for these two "sensors"
                    #"Raw Sensor Reading 1": 100*random.random(),
                    #"Raw Sensor Reading 2": 100*random.random()
                    "Humidity": sense.get_humidity(),
                    "Temperature": (sense.get_temperature_from_humidity() * 9/5 + 32),
                    "Pitch": pitch,
                    "Roll": roll,
                    "Yaw": yaw,
                    "Heading": degreesToNorth,
                    "X Acceleration": accelerationx,
                    "Y Acceleration": accelerationy,
                    "Z Acceleration": accelerationz
                    # If you wanted to read, for example, the digital GPIO pins
                    # 4 and 5 on a Raspberry PI,
                    # you would add to the earlier package import section:
                    # import RPi.GPIO as GPIO
                    # then add the below 3 lines to the above initialize_sensors
                    # function to set up the GPIO pins:
                    # GPIO.setmode(GPIO.BCM)
                    # GPIO.setup(4, GPIO.IN)
                    # GPIO.setup(5, GPIO.IN)
                    # and then lastly, you would change the two Raw Sensor reading lines above to
                    # "Raw Sensor Reading 1": GPIO.input(4),
                    # "Raw Sensor Reading 2": GPIO.input(5)
                }
            ]
        }
    ]

# ************************************************************************
# Helper function: OPTIONAL: updates sense hat display
# ************************************************************************

def update_sense_hat_display(newValue):

    # Append the most recent value to end of the recent values array
    recentReadings.append(newValue)
    # Remove the oldest value from bin 0 of the recent values array
    removedValue = recentReadings.pop(0)
    # Get the max and min values of the recent values array
    maxReading = max(recentReadings)
    minReading = min(recentReadings)

    # Scale all values in the recent values array using the max
    # and the range (max - min); values now range from 0 to 7
    scaledRecentReadings = []
    for i in range(0, len(recentReadings), 1):
        scaledRecentReading = 0
        try:
            scaledRecentReading = int(round(7 * abs(recentReadings[i] - minReading)/abs(maxReading - minReading)))
        except:
            print("Error when computing scaled reading; defaulting to 0")
            
        # Subtract the scaled value from 7, to 'invert' the value,
        # since the LED display is mounted upside-down
        scaledRecentReadings.append(7 - scaledRecentReading)

    # --------------------------------------------

    # Test the hour of day; if it's too late or early, don't show the lights
    currentHour = datetime.datetime.now().hour
    if (NIGHT_MODE_ENABLED and ((currentHour > 22) or (currentHour < 7))):
        # If it's too late or early, sleep 1 second, then turn off the lights
        time.sleep(1)
        sense.clear()
    else:
        # Otherwise, turn on the LEDs!
        # Loop through the array, right to left (7 to 0);
        # This lights up LEDs on the display one column at a time
        for LEDcolumnIndex in range(7, -1, -1):
            # Loop through all 8 LEDs in this column of LEDs
            for LEDrowIndex in range(0, 8, 1):
                # Determine the color for this LED by
                # comparing the row (0-7) that this LED is in
                # to the corresponding scaled recent reading
                if LEDrowIndex >= scaledRecentReadings[LEDcolumnIndex]:
                    # In this case, the row number determines the LED color
                    # Higher row numbers will get "warmer" colors
                    sense.set_pixel(LEDcolumnIndex,LEDrowIndex,
                        RED_TO_GREEN_COLOR_BAR[LEDrowIndex])
                else :
                    # Otherwise, by default, set this LED to the background color
                    sense.set_pixel(LEDcolumnIndex,LEDrowIndex,
                        DEFAULT_BACKGROUND_COLOR)


# ************************************************************************
# Helper function: REQUIRED: wrapper function for sending an HTTPS message
# ************************************************************************

# Define a helper function to allow easily sending web request messages;
# this function can later be customized to allow you to port this script to other languages.
# All it does is take in a data object and a message type, and it sends an HTTPS
# request to the target OMF endpoint
def send_omf_message_to_endpoint(action, message_type, message_json):
    try:
        # Assemble headers that contain the producer token and message type
        # Note: in this example, the only action that is used is "create",
        # which will work totally fine;
        # to expand this application, you could modify it to use the "update"
        # action to, for example, modify existing AF element template types
        web_request_header = {
            'producertoken': PRODUCER_TOKEN,
            'messagetype': message_type,
            'action': action,
            'messageformat': 'JSON',
            'omfversion': '1.0'
        }
        # !!! Note: if desired, ucomment the below line to print the outgoing message
        print('\nOutgoing message: ' + json.dumps(message_json));
        # Send the request, and collect the response; json.dumps is used to
        # properly format the message JSON so that it can be sent as a web request
        response = requests.post(
            TARGET_URL,
            headers=web_request_header,
            data=json.dumps(message_json),
            verify=VERIFY_SSL,
            timeout=WEB_REQUEST_TIMEOUT_SECONDS
        )
        # Print a debug message, if desired; note: you should receive a
        # response code 200 or 202 if the request was successful!
        print(
            'Response from sending a message of type ' +
            '"{0}" with action "{1}": {2} {3}'.format(
                message_type,
                action,
                response.status_code,
                response.text
            )
        )
    except Exception as ex:
        # Log any error, if it occurs
        print(str(datetime.datetime.now()) + " Error during web request: " + str(ex))

# ************************************************************************
# Turn off HTTPS warnings, if desired
# (if the default certificate configuration was used by the PI Connector)
# ************************************************************************

# Suppress insecure HTTPS warnings, if an untrusted certificate is used by the target endpoint
# Remove if targetting trusted targets
if not VERIFY_SSL:
    requests.packages.urllib3.disable_warnings()

print(
    '\n--- Setup: targeting endpoint "' + TARGET_URL + '"...' +
    '\n--- Now sending types, defining containers, and creating assets and links...' +
    '\n--- (Note: a successful message will return a 20X response code.)\n'
)

# ************************************************************************
# Create a JSON packet to define the types of streams that will be sent
# ************************************************************************

DYNAMIC_TYPES_MESSAGE_JSON = [

    # ************************************************************************
    # There are several different message types that will be used by this script, but
    # you can customize this script for your own needs by modifying the types:
    # First, you can modify the "AssetsType", which will allow you to customize which static
    # attributes are added to the new PI AF Element that will be created, and second,
    # you can modify the "DataValuesType", which will allow you to customize this script to send
    # additional sensor values, in addition to (or instead of) the two shown here

    # This values type is going to be used to send real-time values; feel free to rename the
    # values from "Raw Sensor Reading 1" to, say, "Temperature", or "Pressure"
    # Note:
    # all keywords ("id", "type", "classification", etc. are case sensitive!)
    # For a list of the specific keywords used in these messages,
    # see http://omf-docs.readthedocs.io/

    {
        "id": DATA_VALUES_MESSAGE_TYPE_NAME,
        "type": "object",
        "classification": "dynamic",
        "properties": {
            "Time": {
                "format": "date-time",
                "type": "string",
                "isindex": True
            },
            "Humidity": {
                "type": "number"
            },
            "Temperature": {
                "type": "number"
            },
            "Pitch": {
                "type": "number"
            },
            "Roll": {
                "type": "number"
            },
            "Yaw": {
                "type": "number"
            },
            "Heading": {
                "type": "number"
            },
            "X Acceleration": {
                "type": "number"
            },
            "Y Acceleration": {
                "type": "number"
            },
            "Z Acceleration": {
                "type": "number"
            }
            # For example, to allow you to send a string-type live data value,
            # such as "Status", you would add
            #"Status": {
            #   "type": "string"
            #}
        }
    }
]

# ************************************************************************
# Send the DYNAMIC types message, so that these types can be referenced in all later messages
# ************************************************************************

send_omf_message_to_endpoint("create", "Type", DYNAMIC_TYPES_MESSAGE_JSON)

# !!! Note: if sending data to OCS, static types are not included!
if not SEND_DATA_TO_OSISOFT_CLOUD_SERVICES:
    STATIC_TYPES_MESSAGE_JSON = [
        # This asset type is used to define a PI AF Element that will be created;
        # this type also defines two static string attributes that will be created
        # as well; feel free to rename these or add additional
        # static attributes for each Element (PI Point attributes will be added later)
        # The name of this type will also end up being part of the name of the PI AF Element template
        # that is automatically created
        {
            "id": ASSETS_MESSAGE_TYPE_NAME,
            "type": "object",
            "classification": "static",
            "properties": {
                "Name": {
                    "type": "string",
                    "isindex": True
                },
                "Device Type": {
                    "type": "string"
                },
                "Location": {
                    "type": "string"
                },
                "Data Ingress Method": {
                    "type": "string"
                }
                # For example, to add a number-type static
                # attribute for the device model, you would add
                # "Model": {
                #   "type": "number"
                #}
            }
        }
    ]

    # ************************************************************************
    # Send the STATIC types message, so that these types can be referenced in all later messages
    # ************************************************************************

    send_omf_message_to_endpoint("create", "Type", STATIC_TYPES_MESSAGE_JSON)

# ************************************************************************
# Create a JSON packet to define containerids and the type
# (using the types listed above) for each new data events container
# ************************************************************************

# The device name that you specified earlier will be used as the AF Element name!
NEW_AF_ELEMENT_NAME = DEVICE_NAME

CONTAINERS_MESSAGE_JSON = [
    {
        "id": DATA_VALUES_CONTAINER_ID,
        "typeid": DATA_VALUES_MESSAGE_TYPE_NAME
    }
]

# ************************************************************************
# Send the container message, to instantiate this particular container;
# we can now directly start sending data to it using its Id
# ************************************************************************

send_omf_message_to_endpoint("create", "Container", CONTAINERS_MESSAGE_JSON)

# !!! Note: if sending data to OCS, static types are not included!
if not SEND_DATA_TO_OSISOFT_CLOUD_SERVICES:
    # ************************************************************************
    # Create a JSON packet to containing the asset and
    # linking data for the PI AF asset that will be made
    # ************************************************************************

    # Here is where you can specify values for the static PI AF attributes;
    # in this case, we're auto-populating the Device Type,
    # but you can manually hard-code in values if you wish
    # we also add the LINKS to be made, which will both position the new PI AF
    # Element, so it will show up in AF, and will associate the PI Points
    # that will be created with that Element
    ASSETS_AND_LINKS_MESSAGE_JSON = [
        {
            # This will end up creating a new PI AF Element with
            # this specific name and static attribute values
            "typeid": ASSETS_MESSAGE_TYPE_NAME,
            "values": [
                {
                    "Name": NEW_AF_ELEMENT_NAME,
                    "Device Type": (
                        platform.machine() + " - " + platform.platform() + " - " + platform.processor()
                    ),
                    "Location": DEVICE_LOCATION,
                    "Data Ingress Method": "OMF"
                }
            ]
        },
        {
            "typeid": "__Link",
            "values": [
                # This first link will locate such a newly created AF Element under
                # the root PI element targeted by the PI Connector in your target AF database
                # This was specfied in the Connector Relay Admin page; note that a new
                # parent element, with the same name as the PRODUCER_TOKEN, will also be made
                {
                    "Source": {
                        "typeid": ASSETS_MESSAGE_TYPE_NAME,
                        "index": "_ROOT"
                    },
                    "Target": {
                        "typeid": ASSETS_MESSAGE_TYPE_NAME,
                        "index": NEW_AF_ELEMENT_NAME
                    }
                },
                # This second link will map new PI Points (created by messages
                # sent to the data values container) to a newly create element
                {
                    "Source": {
                        "typeid": ASSETS_MESSAGE_TYPE_NAME,
                        "index": NEW_AF_ELEMENT_NAME
                    },
                    "Target": {
                        "containerid": DATA_VALUES_CONTAINER_ID
                    }
                }
            ]
        }
    ]

    # ************************************************************************
    # Send the message to create the PI AF asset; it won't appear in PI AF,
    # though, because it hasn't yet been positioned...
    # ************************************************************************

    send_omf_message_to_endpoint("create", "Data", ASSETS_AND_LINKS_MESSAGE_JSON)

# ************************************************************************
# Initialize sensors prior to sending data (if needed), using the function defined earlier
# ************************************************************************

initialize_sensors()

# ************************************************************************
# Finally, loop indefinitely, sending random events
# conforming to the value type that we defined earlier
# ************************************************************************

print(
    '\n--- Now sending live data every ' + str(NUMBER_OF_SECONDS_BETWEEN_VALUE_MESSAGES) +
    ' second(s) for device "' + NEW_AF_ELEMENT_NAME + '"... (press CTRL+C to quit at any time)\n'
)
if not SEND_DATA_TO_OSISOFT_CLOUD_SERVICES:
    print(
        '--- (Look for a new AF Element named "' + NEW_AF_ELEMENT_NAME + '".)\n'
    )
while True:
    # Call the custom function that builds a JSON object that
    # contains new data values; see the beginning of this script
    VALUES_MESSAGE_JSON = create_data_values_message()

    # Send the JSON message to the target URL
    send_omf_message_to_endpoint("create", "Data", VALUES_MESSAGE_JSON)

    # Send the next message after the required interval
    time.sleep(NUMBER_OF_SECONDS_BETWEEN_VALUE_MESSAGES)
