# Zpotify

<img width="1024" height="585" alt="zpotify3" src="https://github.com/user-attachments/assets/9ec93777-08cf-481a-b06e-ea5fd48afeba" />
<!--Font is modified Eurosoft-->

Zpotify is a tool designed to pull the track list data from a given public Spotify playlist and convert it to a Zune compatible playlist format. It then scans your media folder to look for those tracks based on the names of the directories and files contained. 

There is no installation needed to run Zpotify! Simply run Zpotify.exe and you'll be prompted with next steps.

Zpotify also requires NO ACCOUNT REGISTRATION! You do not need to log in to Spotify or Zpotify to use this software.

### Features to-do prior to release
- [x] Playlist links from file for easy bulk processing
- [x] Switch on/off logging of successful / unsuccessful matches
- [x] Store processed playlists in an archive file with counter for respective successful/unsuccessful matches
- [x] Improve matching by looking for different versions of tracks and verifying correct version with end user (a track may appear on an album, on a greatest hits, on a rerelease, and on a live recording. User should be prompted to verify which would be ideal.)
- [x] reformat config file

### After release plans
- [ ] CLI version (Zpotify is currently a stand-alone/self contained/portable application. The CLI will provide the same arguments and features but allow for custom scripting via cmd/powershell)
- [ ] Support for other playlist types to support other media player options like iTunes, WinAmp, MusicBee, etc.
      
## Commands

| Command  | Definition |
| ------------- | ------------- |
| <Playlist URL/ID> | Entering one playlist will pull that playlist. Entering multiple playlists seperated by commas will process all entered. |
| -h/-help | Prints a help screen with this command info  |
| -p | Prints the path/paths where your music library are read from |
| -p add `<path>` | Adds a path to scan for audio files |
| -p remove `<path>` | Removes a scan path |
| -l | Lists the playlists already downloaded and stored in /playlists |
| exit/quit | Closes the program |

## How To
- When you download Zpotify you will simply have an .exe file. It is recommended you make a folder called Zpotify and place this exe in that folder.
- On first run Zpotify will create a new file called zpotify.config in the directory the application is run from. This file can be updated via Zpotify or through the file itself. If you choose to manually update it and something goes wrong with loading it you can always delete the .config file and Zpotify will generate a new one. To view the config options from within Zpotify run `config`. To open the config file from the application run `config -open`. 
- You will need to update your music folder paths before continuing: Do this by running `-p` to view the current listed directories, then run `-p add <path>` to add a new path to the list. To remove a path run `-p remove <path>`. Paths will be checked at launch for access. Invalid/inaccessible paths will be automatically skipped.
- Zpotify will pull all the tracks from a provided playlist(s). It will then scan your music folder(s) to look for those tracks in your library, create a new folder called Playlists next to your Zpotify files, and finally place in a new .wpl file. This is your playlist file!
- If you choose to turn on logging in the settings you may also get 2 txt documents sharing the name of your playlist - one to show you what files were successfully found, and another to show you which ones were not found. Use this information to help in building out your playlist and fixing issues.
- Simply copy and paste the .wpl file wherever your Zune playlists are stored and read by your Zune software and you'll now be able to use it via the desktop app and your Zune player.

### Bulk playlists
There are 2 ways you can process multiple playlists with Zpotify:

1. You can copy and paste the urls of each playlist into a comma-separated string of urls (ie. playlisturl1,playlisturl2,playlisturl3,etc)
2. You can create a .txt file with each playlist on a separate line then call this playlist within Zpotify

Zpotify does not have the functionality to pull all the playlists from a creator or page, but I have developed a bookmarklet ([more info on bookmarklets](https://www.bookmarkllama.com/blog/bookmarklets?#how-do-you-add-bookmarklets:~:text=Method%202%3A%20Creating%20Manually)) which pulls all the playlists from a page. 

To get this head to [this page](https://github.com/Heroin-Bob/Zpotify/blob/main/Spotify_Playlist_Links_Bookmarklet.js) and click the "Copy raw file" button on the right-hand side. Then make a new bookmark, set the url as the copied text, and save!

Demo:
https://github.com/user-attachments/assets/284dae5b-8bf9-4013-8fd7-e74d20a68910



## Issue Reporting
Use the [Issues](https://github.com/Heroin-Bob/Zpotify/issues) tab at the top of this github to report any issues with the software.

## Inaccuracies and troubleshooting
By the nature of how people store their music files vs looking for tracks with the correct artist and song names in them, it is likely that at some point a song will be found that is either the wrong version or completely incorrect. It is also possible that the song will exist in your music path, but it isn't found by the application. The most likely culpret for this is how you store your music.\
\
When comparing Spotify tracks to files the application does not look at file metadata (this would be wayyyyy too slow to be usable), but rather the directory and file names. If the directory is *../Music/Andrew W.K/Party Hard.mp3* then it will see the artist name in the directory even though it's not part of the file name (and if the artist name is in the file name, that works the same and is perfectly fine). However, if your music is stored as *../Music/3 - Party Hard.mp3*, despite what the metadata reads as, it will not see this track as the one it is looking for.\
\
To simplify - **If you can't look at your file path and immediately tell who the artist is and what the song is called - neither can Zpotify**.
\
It is recommended to use MusicBrainz Picard to update your music library. 

<details>

<summary>More info on recommended settings for MusicBrainz Picard</summary>

## MusicBrainz Picard

To get the best results from your directory scans these are the recommended options (Options > Options) for your music library:

- Under Metadata mark "Convert Unicode puctuation characters to ASCII"
- Under Tags mark "Clear Existing tags"
- Under Tags/ID3 choose ID3v2 Version "2.3"
- Under Cover Art mark "Embed cover images into tags" and "Save cover images as separate files" (leave the file name as "cover")
- Under File Naming mark "Rename files when saving" then click the dropdown beneath it and choose "Preset 2: [album artist]/[album]/[track #]. [title"
- Under File Naming/Compatibility mark "Replace non-ASCII characters"
  
**optional**
- Under User Interface mark "Allow selection of multiple directories"
- Under Advanced mark "Ignore hidden files" and change the "Ignore track duration difference under this number of seconds" to 10


</details>



