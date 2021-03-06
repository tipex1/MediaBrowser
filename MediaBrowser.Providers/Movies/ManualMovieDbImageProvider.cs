﻿using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    class ManualMovieDbImageProvider : IImageProvider
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerConfigurationManager _config;

        public ManualMovieDbImageProvider(IJsonSerializer jsonSerializer, IServerConfigurationManager config)
        {
            _jsonSerializer = jsonSerializer;
            _config = config;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "TheMovieDb"; }
        }

        public bool Supports(IHasImages item)
        {
            return MovieDbImagesProvider.SupportsItem(item);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, ImageType imageType, CancellationToken cancellationToken)
        {
            var images = await GetAllImages(item, cancellationToken).ConfigureAwait(false);

            return images.Where(i => i.Type == imageType);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetAllImages(IHasImages item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            var results = await FetchImages((BaseItem)item, _jsonSerializer, cancellationToken).ConfigureAwait(false);

            if (results == null)
            {
                return list;
            }

            var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.base_url + "original";

            list.AddRange(GetPosters(results).Select(i => new RemoteImageInfo
            {
                Url = tmdbImageUrl + i.file_path,
                CommunityRating = i.vote_average,
                VoteCount = i.vote_count,
                Width = i.width,
                Height = i.height,
                Language = i.iso_639_1,
                ProviderName = Name,
                Type = ImageType.Primary,
                RatingType = RatingType.Score
            }));

            list.AddRange(GetBackdrops(results).Select(i => new RemoteImageInfo
            {
                Url = tmdbImageUrl + i.file_path,
                CommunityRating = i.vote_average,
                VoteCount = i.vote_count,
                Width = i.width,
                Height = i.height,
                ProviderName = Name,
                Type = ImageType.Backdrop,
                RatingType = RatingType.Score
            }));

            var language = item.GetPreferredMetadataLanguage();

            var isLanguageEn = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);

            return list.OrderByDescending(i =>
            {
                if (string.Equals(language, i.Language, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (!isLanguageEn)
                {
                    if (string.Equals("en", i.Language, StringComparison.OrdinalIgnoreCase))
                    {
                        return 2;
                    }
                }
                if (string.IsNullOrEmpty(i.Language))
                {
                    return isLanguageEn ? 3 : 2;
                }
                return 0;
            })
                .ThenByDescending(i => i.CommunityRating ?? 0)
                .ThenByDescending(i => i.VoteCount ?? 0)
                .ToList();
        }

        /// <summary>
        /// Gets the posters.
        /// </summary>
        /// <param name="images">The images.</param>
        /// <returns>IEnumerable{MovieDbProvider.Poster}.</returns>
        private IEnumerable<MovieDbProvider.Poster> GetPosters(MovieDbProvider.Images images)
        {
            return images.posters ?? new List<MovieDbProvider.Poster>();
        }

        /// <summary>
        /// Gets the backdrops.
        /// </summary>
        /// <param name="images">The images.</param>
        /// <returns>IEnumerable{MovieDbProvider.Backdrop}.</returns>
        private IEnumerable<MovieDbProvider.Backdrop> GetBackdrops(MovieDbProvider.Images images)
        {
            var eligibleBackdrops = images.backdrops == null ? new List<MovieDbProvider.Backdrop>() :
                images.backdrops
                .ToList();

            return eligibleBackdrops.OrderByDescending(i => i.vote_average)
                .ThenByDescending(i => i.vote_count);
        }

        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MovieImages}.</returns>
        private async Task<MovieDbProvider.Images> FetchImages(BaseItem item, IJsonSerializer jsonSerializer,
            CancellationToken cancellationToken)
        {
            await MovieDbProvider.Current.EnsureMovieInfo(item, cancellationToken).ConfigureAwait(false);

            var path = MovieDbProvider.Current.GetDataFilePath(item);

            if (!string.IsNullOrEmpty(path))
            {
                var fileInfo = new FileInfo(path);

                if (fileInfo.Exists)
                {
                    return jsonSerializer.DeserializeFromFile<MovieDbProvider.CompleteMovieData>(path).images;
                }
            }

            return null;
        }

        public int Priority
        {
            get { return 2; }
        }
    }
}
