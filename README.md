# Shimamura Bot
A Personal Chatbot for Joystick.TV. Host it from your home computer or remote server (Note VNC or Remote viewer of your choice is required! Opens default webbrowser for OAuth Authorization of resources)

 
## Install
Download the [Latest Release]todo

Next create a `.env` file inside the directory of the bot. This will contain your credentials and some settings such as logging.
The basic requirements for this file are:
```
HOST=THE_HOST_WITH_HTTPS_SCHEME
CLIENT_ID=YOUR_CLIENT_ID
CLIENT_SECRET=YOUR_CLIENT_SECRET
WSS_HOST=THE_WSS_ENDPOINT
```

**NEVER SHARE YOUR `.env` FILE**

For Joystick.TV you will visit [bot application](https://joystick.tv/applications), and scroll to the bottom where it says `Create Bot`.

Important: your `Redirect URL` must be `http://127.0.0.1:8087/auth` as this is a loopback OAuth flow

Fill out the application.  
`HOST` will be the FQDN with http scheme so for Joystick `https://joystick.tv`  
`CLIENT_ID` will be your `OAuth Client ID`  
`CLIENT_SECRET` will be your `OAuth Client Secret`  
`WSS_HOST` can be referenced from [Joystick Support](https://support.joystick.tv/developer_support/) search for `WSS` it should be `wss://joystick.tv/cable`  


Now you're ready to run the application.  
After Authorizing the bot you will see new entries in your `.env` file.  
Currently the only optional one is `LOGGING` which defaults to `False`, which you can change to `True` if you wish to enable logging of chat/events



## Compile
<p>TODO</p><br /><br />




[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/adachi91)