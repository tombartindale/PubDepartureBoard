# Pub Departure Board

Windows Universal application which displays live UK train departure information based on a particular station. Indicates when you should get a drink, leave and have missed trains based on their predicted arrival time and distance from the venue.

# Installation

1. Install Windows IoT onto an embedded device (tested on RPi3) https://developer.microsoft.com/en-us/windows/iot/getstarted/prototype/setupdevice
2. Follow the instructions to setup your device on your network (WiFi or Ethernet).
3. Visit the admin panel of the device (using IoT Core Dashboard) https://developer.microsoft.com/en-us/windows/iot/Downloads
4. Install the application from the Downloads link.
5. Set the application to be the Main Application when the device starts.
5. Set the screen resolution to match your output display.
6. Restart the device.
7. Obtain an API key from https://darksky.net/dev
8. Obtain an API key from http://realtime.nationalrail.co.uk/OpenLDBWSRegistration/
6. Visit the link displayed on the screen in your browser and enter the credentials / information that matches your installation.