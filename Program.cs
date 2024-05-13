using SpotifyPrivate.Album;
using SpotifyAPI.Web;
using static SpotifyAPI.Web.PlayerResumePlaybackRequest;
using SpotifyPrivateAPI.src;

class Program
{
    // Get these from https://developer.spotify.com/dashboard
    private static string CLIENT_ID = "YOUR_CLIENT_ID";
    private static string SECRET_KEY = "YOUR_SECRET_KEY";

    // Using a second client to get the Play Counts
    private static SpotifyPrivate.API _spotifyPrivateClient;
    private static SpotifyClient _spotifyClient;

    // Storing the artist ID for Ilaiyaraja to fetch all his albums
    private static string _ilaiyarajaArtistId = "3m49WVMU4zCkaVEKb8kFW7";

    static async Task Main()
    {
        await InitializeSpotifyClients();

        var albums = await GetAllAlbumsByArtistAsync(_ilaiyarajaArtistId);
        albums.Reverse(); // Reversing the albums array to get the oldest albums first

        Console.WriteLine("\n Showing all albums by Ilaiyaraja:");
        ShowAlbums(albums);


        int NUMBER_OF_ALBUMS_TO_PRINT_FOR_DEMO = 5;
        for(int i = 0;i < NUMBER_OF_ALBUMS_TO_PRINT_FOR_DEMO; i++) 
        {
            var album = albums[i];
            Console.WriteLine($"Getting all tracks from: {album.Name}");
            var tracks = await GetAllTracksByAlbumAsync(album.Id);
            
            // Also make a new request to get PlayCount using the Private Client
            foreach(var track in tracks)
            {
                var info = await GetTrackInfoAsync(_spotifyPrivateClient, track.Id);
                //Console.WriteLine($"Play Count: {info.Playcount}");
                track.PlayCount = info.Playcount;
            }

            ShowAllTracks(tracks);
        }
        Console.WriteLine("--------------------------------------------------");
    }

    private async static void ShowAllTracks(List<ComplexTrack> tracks)
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            //var trackInfoFromPrivateClient = await GetTrackInfoAsync(_spotifyPrivateClient, track.Id);
            Console.WriteLine($"{i + 1}. {track.Name} : {track.Id} (Play Count: {track.PlayCount})");
        }
        Console.WriteLine();
    }

    private static void ShowAlbums(List<SimpleAlbum> albums)
    {
        for (int i = 0; i < albums.Count; i++) 
        {
            var album = albums[i];
            Console.WriteLine($"{i + 1}. {album.Name} : {album.Id} : {album.ReleaseDate}");
        }
        Console.WriteLine();
    }

    private static async Task InitializeSpotifyClients()
    {
        _spotifyPrivateClient = new SpotifyPrivate.API();

        var config = SpotifyClientConfig.CreateDefault();
        var request = new ClientCredentialsRequest(CLIENT_ID, SECRET_KEY);
        var response = await new OAuthClient(config).RequestToken(request);
        _spotifyClient = new SpotifyClient(config.WithToken(response.AccessToken));
    }

    static async Task<Track> GetTrackInfoAsync(SpotifyPrivate.API spotify, string trackId)
    {
        var track = await spotify.GetTrack(trackId);

        if (track == null)
        {
            Console.WriteLine($"Track not found: {trackId}");
            return null;
        }
        return new Track
        {
            Name= track.Data.TrackUnion.Name,
            Playcount = track.Data.TrackUnion.Playcount,
        };
    }

    public static async Task<List<SimpleAlbum>> GetAllAlbumsByArtistAsync(string artistId)
    {
        var albums = new List<SimpleAlbum>();
        int offset = 0; // Initial offset is set to 0
        while (true)
        {
            var req = new ArtistsAlbumsRequest
            {
                Limit = 50,
                Offset = offset, // Set the offset for pagination
                IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Album
            };

            var albumsToAdd = await _spotifyClient.Artists.GetAlbums(artistId, req);

            if (albumsToAdd.Items.Count == 0)
            {
                break; // Break the loop if no albums are returned
            }

            // Process each album
            foreach (var album in albumsToAdd.Items)
            {
                try
                {
                    albums.Add(album);
                    offset++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break; // Break out of the loop in case of an exception
                }
            }

            // Check if the number of albums fetched is less than the limit, which means no more albums are left
            if (albumsToAdd.Items.Count < 50)
            {
                break;
            }
        }
        return albums;
    }

    public static async Task<List<ComplexTrack>> GetAllTracksByAlbumAsync(string albumId)
    {
        var tracks = new List<ComplexTrack>();
        var req = new AlbumTracksRequest
        {
            Limit = 50 // Maximum limit
        };
        Paging<SimpleTrack> albumTracks = await _spotifyClient.Albums.GetTracks(albumId, req);

        // Collect tracks and handle pagination
        while (true)
        {
            //tracks.AddRange(albumTracks.Items);
            foreach(var track in albumTracks.Items)
            {
                tracks.Add(new ComplexTrack(track));
            }
            if (albumTracks.Next == null || albumTracks.Items.Count == 0)
                break;
        }
        return tracks;
    }
}
