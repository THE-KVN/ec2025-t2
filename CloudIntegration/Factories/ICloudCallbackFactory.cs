using System;
using Zooscape.Infrastructure.CloudIntegration.Enums;
using Zooscape.Infrastructure.CloudIntegration.Models;

namespace Zooscape.Infrastructure.CloudIntegration.Factories;

public interface ICloudCallbackFactory
{
    CloudCallback Build(
        CloudCallbackType callbackType,
        Exception? e = null,
        int? seed = null,
        int? ticks = null
    );
}
