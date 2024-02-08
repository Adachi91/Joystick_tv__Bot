# Shimamura Bot
A Personal Chatbot for Joystick.TV. Host it from your home computer or remote server (Note VNC or Remote viewer of your choice is required! Opens default webbrowser for OAuth Authorization of resources)

 
## Install
Download the [Latest Release]todo

```
HOST=THE_HOST_WITH_HTTPS_SCHEME
CLIENT_ID=YOUR_CLIENT_ID
CLIENT_SECRET=YOUR_CLIENT_SECRET
WSS_HOST=THE_WSS_ENDPOINT
```
Extract the Files to the Folder of your choice.  
Open up your `.env` File in the Text Editor of your choice for the next step.  

**NEVER SHARE YOUR `.env` FILE**

For Joystick.TV you will visit [bot application](https://joystick.tv/applications), and scroll to the bottom where it says `Create Bot`.

Important: your `Redirect URL` must be `http://127.0.0.1:8087/auth` as this is a loopback OAuth flow

Fill out the application.  
`HOST` will be the FQDN with http scheme so for Joystick `https://joystick.tv`  
`CLIENT_ID` will be your `OAuth Client ID`  
`CLIENT_SECRET` will be your `OAuth Client Secret`  
`WSS_HOST` can be referenced from [Joystick Support](https://support.joystick.tv/developer_support/) search for `WSS` it should be `wss://joystick.tv/cable`  

Now when you have all the entries in your `.env` filled out with your bot application values, you're ready to run the bot.

**Commands:**  
- `logging` toggles logging on/off to `shimamura.log`
- `help` (display all options)
- `start` (start the bot)
- `stop` (stop the bot)

<p>if you run into any problems please submit an [issue here](#issues)</p>

## Compile
<p>TODO</p><br /><br />




[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/adachi91)