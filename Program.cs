using SpotifyPrivate.Album;
using SpotifyAPI.Web;
using SpotifyPrivateAPI.src;
using Newtonsoft.Json;
using IdentityModel;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.Extensions;


class Program
{
    // Get these from https://developer.spotify.com/dashboard
    private static string CLIENT_ID = "f795b603c81b482bb4c2ac19642fa31b";
    private const string RedirectUri = "http://localhost/"; // For now set to http://localhost for my Spotify client reg.

    // Using a second client to get the Play Counts
    private static SpotifyPrivate.API _spotifyPrivateClient;
    private static SpotifyClient _spotifyClient;

    // Storing the artist ID for Yuvan to fetch all his albums
    //private static string _yuvanArtistId = "29aw5YCdIw2FEXYyAJZI8l";

    private static string _playlistID = "0Fj8jF7WnhGsK3m5hfjc4L";

    static async Task Main()
    {
        await InitializeSpotifyClients();

        var albums = GetAllAlbumsFromFile("./data/albumsToGet.json");
        foreach (var album in albums)
        {
            var albumID = await GetAlbumIdFromSpotifyAsync(_spotifyClient, album.Name);
            if (!string.IsNullOrEmpty(albumID))
            {
                album.Id = albumID;
            }

            WriteBoth($"To fetch album {album.Name} with Spotify ID {album.Id}");
        }

        int NUMBER_OF_ALBUMS_TO_PROCESS = albums.Count;

        // Ensure the directory exists before opening the StreamWriter
        string directoryPath = "../data";
        string filePath = Path.Combine(directoryPath, "addedSongs.txt");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Open a StreamWriter to overwrite the existing file or create a new one if it doesn't exist
        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            for (int i = 0; i < NUMBER_OF_ALBUMS_TO_PROCESS; i++)
            {
                var album = albums[i];
                if (string.IsNullOrEmpty(album.Id)) continue;
                WriteBoth($"Getting all tracks from: {album.Name}", writer);
                var tracks = await GetAllTracksByAlbumAsync(album.Id);

                foreach (var track in tracks)
                {
                    var info = await GetTrackInfoAsync(_spotifyPrivateClient, track.Id);
                    if (info != null)
                    {
                        track.PlayCount = int.Parse(info.Playcount);
                    }
                }
                var top3Tracks = GetTopNTracksFromAlbum(tracks, 3);

                // Write the top 3 tracks to the file and console
                writer.WriteLine($"Album: {album.Name}");
                Console.WriteLine($"Album: {album.Name}");
                foreach (var track in top3Tracks)
                {
                    WriteBoth($"Track: {track.Name}, PlayCount: {track.PlayCount}", writer);
                }
                await AddTracksToPlaylistAsync(top3Tracks);
                WriteBoth("--------------------------------------------------", writer);
            }
        }

        WriteBoth("--------------------------------------------------");
    }

    // Helper method to write to both console and a StreamWriter
    static void WriteBoth(string message, StreamWriter writer = null)
    {
        Console.WriteLine(message);
        writer?.WriteLine(message);
    }

    static List<SimpleAlbum> GetAllAlbumsFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var albumData =  JsonConvert.DeserializeObject<List<GivenJsonAlbum>>(json);
        List<SimpleAlbum> albumsToReturn = new();
        foreach(var album in albumData)
        {
            albumsToReturn.Add(new SimpleAlbum
            {
                Name = album.name,
            }) ;
        }
        return albumsToReturn;
    }

    static async Task<string> GetAlbumIdFromSpotifyAsync(SpotifyClient spotify, string albumName)
    {
        var searchRequest = new SearchRequest(SearchRequest.Types.Album, albumName);
        var searchResponse = await spotify.Search.Item(searchRequest);

        if (searchResponse.Albums.Items.Count > 0)
        {
            foreach (var album in searchResponse.Albums.Items)
            {
                return album.Id;
            }
        }

        return null;
    }

    private async static Task AddTracksToPlaylistAsync(List<ComplexTrack> tracksToAdd, int position = -1)
    {
        List<string> URIs = tracksToAdd.Select(track => track.Uri.ToString()).ToList();
        var req = new PlaylistAddItemsRequest(URIs);
        if (position != -1)
        {
            req.Position = position;
        }
        var res = await _spotifyClient.Playlists.AddItems(_playlistID, req);
    }

    private static List<ComplexTrack> GetTopNTracksFromAlbum(List<ComplexTrack> tracks, int n)
    {
        return tracks.OrderByDescending(track => track.PlayCount).Take(n).ToList();
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

        string codeVerifier = CryptoRandom.CreateUniqueId(64);
        Console.WriteLine($"Code Verifier: {codeVerifier}");

        string codeChallenge = GenerateCodeChallenge(codeVerifier);
        Console.WriteLine($"Code Challenge: {codeChallenge}");

        string scope = "user-read-private playlist-read-private playlist-read-collaborative playlist-modify-private playlist-modify-public";
        var authUrl = new UriBuilder("https://accounts.spotify.com/authorize")
        {
            Query = new QueryBuilder()
            {
                { "response_type", "code" },
                { "client_id", CLIENT_ID },
                { "scope", scope },
                { "redirect_uri", RedirectUri },
                { "code_challenge_method", "S256" },
                { "code_challenge", codeChallenge }
            }.ToString()
        };

        Console.WriteLine("Open the following URL in your browser:");
        Console.WriteLine(authUrl.ToString());

        // Instruct the user to manually copy the authorization code from the redirected URL
        Console.WriteLine("After authorizing the application, you will be redirected to a URL.");
        Console.WriteLine("Copy the 'code' parameter from that URL and paste it below.");

        // Assume user has authenticated and we have received the authorization code
        Console.Write("Enter the authorization code: ");
        var authorizationCode = Console.ReadLine();

        var tokenResponse = await RequestTokenAsync(authorizationCode, codeVerifier);
        _spotifyClient = new SpotifyClient(tokenResponse.AccessToken);

        // Now you can use the spotifyClient to interact with the Spotify API
        Console.WriteLine("Access token obtained. You can now use the Spotify API.");
    }

    static async Task<PKCETokenResponse> RequestTokenAsync(string authorizationCode, string codeVerifier)
    {
        var tokenRequest = new PKCETokenRequest(
            CLIENT_ID,
            authorizationCode,
            new Uri(RedirectUri),
            codeVerifier
        );

        var config = SpotifyClientConfig.CreateDefault();
        var tokenClient = new OAuthClient(config);
        var response = await tokenClient.RequestToken(tokenRequest);

        return response;
    }

    static string GenerateCodeChallenge(string codeVerifier)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.ASCII.GetBytes(codeVerifier);
            var hash = sha256.ComputeHash(bytes);
            return Base64UrlEncode(hash);
        }
    }

    static string Base64UrlEncode(byte[] input)
    {
        var output = Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return output;
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