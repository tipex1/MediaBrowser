﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv
{
    public interface ILiveTvRecording : IHasImages, IHasMediaStreams
    {
        string ServiceName { get; set; }

        string MediaType { get; }

        LocationType LocationType { get; }

        RecordingInfo RecordingInfo { get; set; }

        string GetClientTypeName();

        string GetUserDataKey();

        bool IsParentalAllowed(User user);

        Task<bool> RefreshMetadata(CancellationToken cancellationToken, bool forceSave = false, bool forceRefresh = false, bool allowSlowProviders = true, bool resetResolveArgs = true);
    }
}
