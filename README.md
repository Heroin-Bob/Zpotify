# Zpotify

<img width="1024" height="585" alt="zpotify3" src="https://github.com/user-attachments/assets/9ec93777-08cf-481a-b06e-ea5fd48afeba" />
<!--Font is modified Eurosoft-->

Zpotify is a tool designed to pull the track list data from a given public Spotify playlist and convert it to a Zune compatible playlist format. It then scans your media folder to look for those tracks based on the names of the directories and files contained. 

There is no installation needed to run Zpotify! Simply run Zpotify.exe and you'll be prompted with next steps.

Zpotify also requires NO ACCOUNT REGISTRATION! You do not need to log in to Spotify or Zpotify to use this software.

### Features to-do prior to release
- [ ] Playlist links from file for easy bulk processing
- [ ] Option to switch to directory archive to speed up checks in the future (a file will be created with a record of your directory which will be referenced instead of scanning your music folder every time it needs to process a playlist making pulling new playlists take as little as 3 seconds)
- [ ] Switch on/off logging of successful / unsuccessful matches
- [ ] Store processed playlists in an archive file with counter for respective successful/unsuccessful matches
- [ ] reformat config file

### After release plans
- [ ] CLI version (Zpotify is currently a stand-alone/self contained/portable application. The CLI will provide the same arguments and features but allow for custom scripting via cmd/powershell)
- [ ] Support for other playlist types to support other media player options like iTunes, WinAmp, MusicBee, etc.
      
## Commands

| Command  | Definition |
| ------------- | ------------- |
| <Playlist URL/ID> | Entering one playlist will pull that playlist. Entering multiple playlists seperated by commas will process all entered. |
| -h/-help | Prints a help screen with this command info  |
| -p | Prints the path/paths where your music library are read from |
| -p add `<path>` | Adds a path to a folder to scan for audio files |
| -p remove `<path>` | Removes a scan path |
| -l | Lists the playlists already downloaded and stored in /playlists |
| exit/quit | Closes the program |

## How To
- When you download Zpotify you will simply have an .exe file. It is recommended you make a folder called Zpotify and place this exe in that folder.
- On first run the Zpotify.exe file will create a new file called zpotify.config. You will need to locate this file and open it in notepad or another text editor (Word is not recommended).
- Remove everything in this file and add in the full path to your music folder (ie, C:\Users\Test\Music, \\192.168.1.12\Music, etc.). Network paths are supported but they may need to be added via a network drive or network folder. If you have more than one folder to scan, add only one per line.
- Once this is updated you can now use Zpotify! Open Zpotify.exe again and paste in your first playlist.
- Zpotify will pull down all the tracks from the playlist, then it will scan your music folder(s) to look for those tracks in your library, then it will create a new folder called Playlists next to your Zpotify files and place in a new .wpl file. This is your playlist file! You'll also get 2 text documents - one to show you what files were successfully found, and another to show you which ones were not found. Use this information to help in building out your playlist and fixing issues.
- Simply copy and paste the .wpl file wherever your Zune playlists are stored and read by your Zune software and you'll now be able to use it via the desktop app and your Zune player.

## Inaccuracies and troubleshooting
By the nature of how people store their music files vs looking for tracks with the correct artist and song names in them, it is likely that at some point a song will be found that is either the wrong version or completely incorrect. It is also possible that the song will exist in your music path, but it isn't found by the application. The most likely culpret for this is how you store your music.\
\
When comparing Spotify tracks to files the application does not look at file metadata (this would be wayyyyy too slow to be usable), but rather the directory and file names. If the directory is *../Music/Andrew W.K/Party Hard.mp3* then it will see the artist name in the directory even though it's not part of the file name (and if the artist name is in the file name, that works the same and is perfectly fine). However, if your music is stored as *../Music/3 - Party Hard.mp3*, despite what the metadata reads as, it will not see this track as the one it is looking for.\
\
To simplify - **If you can't look at your file path and immediately tell who the artist is and what the song is called - neither can Zpotify**.\
\
If you're having issues with your file paths then I recommend MusicBranz Picard to help straighten up your music catalog. Be sure to use a profile setting that will make your directory something similar to *Artist/album/artist - tracknumber. track.ext*. And, while the scanning is really good, I recommend taking your time with it and not try to rush it. Mistakes in renaming your files can happen.



