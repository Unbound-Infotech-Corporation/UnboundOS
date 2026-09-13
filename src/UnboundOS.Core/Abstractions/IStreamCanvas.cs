using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IStreamCanvas
{
    StreamCropPlan BuildCropPlan(StreamPreferences preferences);
    string BuildObsInstructions(StreamCropPlan plan, StreamPreferences preferences);
}
