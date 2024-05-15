using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPrivateAPI.src
{
    public class ComplexTrack : SimpleTrack
    {
        public int PlayCount;

        public ComplexTrack(SimpleTrack track) : base()
        {
            if (track == null)
            {
                throw new ArgumentNullException(nameof(track), "Provided SimpleTrack is null");
            }

            // Copy all properties from SimpleTrack to ComplexTrack
            //this.Album = track.album;
            this.Artists = track.Artists;
            this.AvailableMarkets = track.AvailableMarkets;
            this.DiscNumber = track.DiscNumber;
            this.DurationMs = track.DurationMs;
            this.Explicit = track.Explicit;
            this.ExternalUrls = track.ExternalUrls;
            this.Href = track.Href;
            this.Id = track.Id;
            //this.IsLocal = track.IsLocal;
            this.IsPlayable = track.IsPlayable;
            this.LinkedFrom = track.LinkedFrom;
            this.Name = track.Name;
            this.PreviewUrl = track.PreviewUrl;
            this.TrackNumber = track.TrackNumber;
            this.Type = track.Type;
            this.Uri = track.Uri;
            // Additional initialization or transformation logic can be added here
        }
    }
}
