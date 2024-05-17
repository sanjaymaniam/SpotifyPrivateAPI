# TinySpotifyManager

This is a little program to find all the hit songs from any artist you like and populate a playlist with those songs.
For instance, if you're into Harris Jayaraj, you can list all his albums in a .json file, and this program will dig through those albums, pick out the hits, and add them to a Spotify playlist you specify.
It's like making your own "greatest hits" playlist without needing to manually search for each track.

You can also make use of the available methods to manage your playlists.

This playlist, for example, was created with this util and modified to include/exlude some Harris tracks:
https://open.spotify.com/playlist/4oi3TiZKftxZMNqsUcROJS?si=b793ff4998d847af

## Usage

- Populate the `albumsToGet.json` file with all the albums you want to include.
- Sign up for Spotify API access and get your Client ID, Secret Key, and a redirect URI ready.
- Update the fields in Program.cs with the Client ID, Secret Key, and a redirect URI.
- Update the Playlist ID.
- Compile and run this program.
- You'll be asked to login to your Spotify account on your browser. After you log in, you'll be redirected to the specified URI.
- Copy the auth token from the redirect URI and paste into this program's console

## How it works

Routes are based on [Spotify Web Player](https://open.spotify.com/) API, all requests use anynomous token to get informations

## Available methods

- GetAllAlbumsFromFile: Reads a JSON file containing album names and converts it into a list of SimpleAlbum objects.
- GetAlbumIdAsync: Searches for an album on Spotify using its name and returns the album's Spotify ID.
- AddTracksToPlaylist: Adds a list of tracks to a specified Spotify playlist.
- GetTopNTracksFromAlbum: Sorts tracks by play count and retrieves the top 'N' tracks from the list.
- GetTrackInfoAsync: Fetches detailed information about a track using the private Spotify API, including the play count.
- GetAllAlbumsByArtistAsync: Retrieves all albums for a specified artist ID from Spotify. (the response from the Spotify API for this was not reliable- many albums were skipped, hence we manually populate the albumsToGet.json file).
- GetAllTracksByAlbumAsync: Fetches all tracks from a specified album ID.

## TO DO
- [ ] Avoid having to login each time- store the auth token and use the refresh token.
- [ ] Scrape the artist's Wikipedia page for their complete discography and populate albumsToGet.json automatically. Just accepting the artist name as an input should be enough.
- [ ] Rename the repo to TinySpotifyManager.
- [ ] After I add some songs to the playlist, I want the playlist to remain sorted. Placing each track in the right place in the Spotify app is tedious- I'd like to have a sort function here that sorts the playlist. (Clear all the tracks, push the sorted set of tracks to the playlist. Spotify API does not support sorting the playlist directly at the moment.)