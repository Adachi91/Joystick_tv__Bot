# Shimamura Bot
A Personal Chatbot for Joystick.TV. Host it from your home computer or remote server (Note VNC or Remote viewer of your choice is required! Opens default webbrowser for OAuth Authorization of resources).  
It also supports multiple streaming services, and VTuber software support (such as vNyan, and soon VTuberStudio) for nodes like item throwing, or custom events.  
Works for Linux and Windows.

## Features
* Joystick.TV chat integration.
* Joystick.TV API integration (Change `stream_title`, `chat_greeting`, add/remove `banned_words`).
* Pseudo point system (Similar to 'Channel Points').
* Reward-System for points (e.g. Throw rubby ducky at VTuber Model).
* Game Modules (e.g. Over/Under).
* Discord Webhook (Announce when you go live).
* Twitch Chat integration.
* Basic Twitch Channel management.

## Coming Soon
* WebUI that combines all chats from services on to 1 page, and settings.
* VTuberStudio Support.
* Public-Facing WebUI (Hosted on a server :80) with authorization to protect it.
* (Maybe) more Twitch API integrations.
  
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

**Important**: your `Redirect_URL` must be `http://127.0.0.1:8087/auth` as this is a loopback OAuth flow

Fill out the application.  
`HOST` will be the FQDN with http scheme so for Joystick `https://joystick.tv`  
`CLIENT_ID` will be your `OAuth2 Client ID`  
`CLIENT_SECRET` will be your `OAuth2 Client Secret`  
`WSS_HOST` can be referenced from [Joystick Support](https://support.joystick.tv/developer_support/) search for `WSS` it should be `wss://joystick.tv/cable`  

Now when you have all the entries in your `.env` filled out with your bot application values, you're ready to run the bot.

**Commands:**  
- `logging` toggles logging on/off to `shimamura.log`
- `help` (display all options)
- `start` (start the bot)
- `stop` (stop the bot)

<p>if you run into any problems please submit an [issue here](../../issues)</p>

## Compile
<p>TODO</p>  
May Zeus help you if you try and read my code style / improper usage.  
Crackhead Coding:tm: Engineer (sounds fancy) since 2002  
<br /><br />




[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/adachi91)