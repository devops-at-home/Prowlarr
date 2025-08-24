using System;
using System.Net;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Download.Clients.RQBit.ResponseModels;

namespace NzbDrone.Core.Download.Clients.RQBit
{
    public interface IRQbitProxy
    {
        string GetVersion(RQbitSettings settings);
        string AddTorrentFromUrl(string torrentUrl, RQbitSettings settings);
        string AddTorrentFromFile(string fileName, byte[] fileContent, RQbitSettings settings);
    }

    public class RQbitProxy : IRQbitProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public RQbitProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public string GetVersion(RQbitSettings settings)
        {
            var request = BuildRequest(settings).Resource("");
            var response = _httpClient.Get(request.Build());

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var rootResponse = JsonConvert.DeserializeObject<RootResponse>(response.Content);
                return rootResponse.Version;
            }
            else
            {
                _logger.Error("Failed to get RQBit version");
            }

            return string.Empty;
        }

        public string AddTorrentFromUrl(string torrentUrl, RQbitSettings settings)
        {
            var itemRequest = BuildRequest(settings).Resource("/torrents?overwrite=true").Post().Build();
            itemRequest.SetContent(torrentUrl);
            var httpResponse = _httpClient.Post(itemRequest);

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var response = JsonConvert.DeserializeObject<PostTorrentResponse>(httpResponse.Content);

            if (response.Details == null)
            {
                return null;
            }

            return response.Details.InfoHash;
        }

        public string AddTorrentFromFile(string fileName, byte[] fileContent, RQbitSettings settings)
        {
            var itemRequest = BuildRequest(settings)
                .Post()
                .Resource("/torrents?overwrite=true")
                .Build();
            itemRequest.SetContent(fileContent);
            var httpResponse = _httpClient.Post(itemRequest);

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var response = JsonConvert.DeserializeObject<PostTorrentResponse>(httpResponse.Content);

            if (response.Details == null)
            {
                return null;
            }

            return response.Details.InfoHash;
        }


        private HttpRequestBuilder BuildRequest(RQbitSettings settings)
        {
            var requestBuilder = new HttpRequestBuilder(settings.UseSsl, settings.Host, settings.Port, settings.UrlBase)
            {
                LogResponseContent = true,
            };

            return requestBuilder;
        }
    }
}
