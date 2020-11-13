// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Remote
{
    internal static class RazorRemoteUtilities
    {
        private static readonly JsonSerializer SolutionInfoDeserializer;

        static RazorRemoteUtilities()
        {
            SolutionInfoDeserializer = JsonSerializer.Create(new JsonSerializerSettings() { Converters = new[] { AggregateJsonConverter.Instance }, DateParseHandling = DateParseHandling.None });
        }

        public static async ValueTask<Solution> GetSolutionAsync(ServiceBrokerClient client, IServiceProvider serviceProvider, object solutionInfo, CancellationToken cancellationToken)
        {
            var pinnedSolutionInfo = solutionInfo as PinnedSolutionInfo;
            if (pinnedSolutionInfo == null)
            {
                if (solutionInfo is JObject solutionInfoJOBject)
                {
                    var reader = solutionInfoJOBject.CreateReader();
                    pinnedSolutionInfo = SolutionInfoDeserializer.Deserialize<PinnedSolutionInfo>(reader);
                }
                else
                {
                    throw new InvalidOperationException("Unexpected solution info type");
                }
            }

            using var rental = await client.GetProxyAsync<IRemoteWorkspaceSolutionProviderService>(RemoteWorkspaceSolutionProviderService.ServiceDescriptor, cancellationToken).ConfigureAwait(false);
            //var provider = (IRemoteWorkspaceSolutionProviderService)serviceProvider.GetService(typeof(IRemoteWorkspaceSolutionProviderService));

            Contract.ThrowIfNull(rental.Proxy);
            var solution = await rental.Proxy.GetSolutionAsync(pinnedSolutionInfo, cancellationToken).ConfigureAwait(false);
            //var solution = await provider.GetSolutionAsync(pinnedSolutionInfo, cancellationToken).ConfigureAwait(false);
            return solution;
        }
    }
}
