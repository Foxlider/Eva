![GitHub release](https://img.shields.io/github/release/Foxlider/Eva.svg?style=flat-square)
![GitHub](https://img.shields.io/github/license/Foxlider/Eva.svg?style=flat-square)
![AppVeyor](https://img.shields.io/appveyor/ci/Foxlider/Eva.svg?logo=appveyor&style=flat-square)

# [ E.V.A. ]
Eva Bot created to replace EDPostcards_BOT 
Makes the link between the Twitter Community and its Discord Server
Written in C# - .NET Core 3.0 

# [ Installation ]
Pretty much just build it with dotnet

# [ Use ]
Publish some pictures to twitter with the #EDPostcards or by quoting @ED_Postcards

# [ Commands ]
 - version		Displays the version of the bot
 - quote		Quote a specified tweet
 - help			Displays the bot's help on commands
 - logLvl		Changes the severity level of console messages
 - role			Give the user the Photograph Role
 - send			Send pictures to Twitter.

 # [ Changelog ]

## [ Version 1 ]  
###   [ v1.0 ]
 - Initial release  
###   [ v1.1 ]  
Added :
  - Added Discord Support for the bot
  - Added Configuration manager
  - App Exit manager 

Fixed : 
  - Some minor code fixes  
  - Some minor typo fixes  
  - Fixed tests configuration
  - Tests compatibility with appveyor

###   [ v1.2 ]  
Added : 
 - Command role to give a user the photograph role
 - Command send to send pictures from Discord to twitter
 - RequireRole Precondition

Fixed : 
 - Fixed DownloadMedia would download uncompatible Medias

### [ v1.3 ]
Added :
 - Command role to give a user the photograph role
 - Command send to send pictures from Discord to twitter
 - RequireRole Precondition

Fixed :
 - Fixed DownloadMedia would download uncompatible Medias

### [ v1.4 ]
Added :
 - Check command to check if a tweet is able to be quoted
 - Thread watcher and Twitter Restarter
 - Icon
 - Improved logs to Console

Fixed :
 - Crash on logging to file

### [ v1.5 ]
Added :
 - Improved logs to Console

Fixed :
 - Fixed log time to 24h format
 - Fixed minor text issues