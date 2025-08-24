using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.RQBit
{
    public class RQBit : TorrentClientBase<RQbitSettings>
    {
        private readonly IRQbitProxy _proxy;

        public override string Name => "RQBit";
        public override bool SupportsCategories => false;

        public RQBit(IRQbitProxy proxy,
            ITorrentFileInfoReader torrentFileInfoReader,
            ISeedConfigProvider seedConfigProvider,
            IConfigService configService,
            IDiskProvider diskProvider,
            ILocalizationService localizationService,
            Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, localizationService, logger)
        {
            _proxy = proxy;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestVersion());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to connect to RQBit");
                return new ValidationFailure("", "Unable to connect to RQBit. Please check that RQBit is running and accessible.");
            }
        }

        private ValidationFailure TestVersion()
        {
            try
            {
                var versionString = _proxy.GetVersion(Settings);
                
                if (string.IsNullOrWhiteSpace(versionString))
                {
                    return new ValidationFailure("", "Unable to determine RQBit version. Please check that RQBit is running and accessible.");
                }

                // Parse version string to Version object
                var versionMatch = System.Text.RegularExpressions.Regex.Match(versionString, @"(\d+)\.(\d+)\.(\d+)");

                if (!versionMatch.Success)
                {
                    return new ValidationFailure("", $"Unable to parse RQBit version '{versionString}'. Please check that RQBit is running and accessible.");
                }

                var major = int.Parse(versionMatch.Groups[1].Value);
                var minor = int.Parse(versionMatch.Groups[2].Value);
                var patch = int.Parse(versionMatch.Groups[3].Value);
                var apiVersion = new Version(major, minor, patch);

                var minimumVersion = new Version(8, 0, 0);

                if (apiVersion < minimumVersion)
                {
                    return new ValidationFailure("", $"RQBit version {apiVersion} is not supported. Please upgrade to version {minimumVersion} or higher.");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to determine RQBit version");
                return new ValidationFailure("", "Unable to determine RQBit version. Please check that RQBit is running and accessible.");
            }
        }

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            return _proxy.AddTorrentFromUrl(magnetLink, Settings);
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            return _proxy.AddTorrentFromFile(filename, fileContent, Settings);
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            return _proxy.AddTorrentFromUrl(torrentLink, Settings);
        }
    }
}
