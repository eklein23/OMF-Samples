import os
from machine import UART
from network import WLAN
import machine

# Enable REPL over UART
uart = UART(0, 115200)
os.dupterm(uart)

# Specify Wi-Fi credentials
wlan = WLAN()
ssid = 'thumper2.4'
password = 'concretebunkerbiggameday'

# Function for initializing Wi-Fi
def wifi_init():
    wlan.mode(WLAN.STA)
    wlan.ifconfig(config='dhcp')  # Uncomment this for dynamic ipd

# Function for establishing a connection
def wifi_connect():
    wlan.connect(ssid, auth=(WLAN.WPA2, password), timeout=5000)
    while not wlan.isconnected():
        machine.idle()
    cfg = wlan.ifconfig()
    print('WLAN connected to ip {} gateway {}'.format(cfg[0], cfg[2]))

# Main function that calls the Wi-Fi connect and establishing code
def wifi_set():
    wifi_init()
    if not wlan.isconnected():
        wifi_connect()

# Finally, run the main function
wifi_set()
