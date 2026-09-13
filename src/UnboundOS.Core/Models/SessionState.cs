namespace UnboundOS.Core.Models;

public enum SessionState
{
    Idle = 0,
    Entering = 1,
    Active = 2,
    Exiting = 3,
    Faulted = 4
}

public enum ProfileKind
{
    Competitive = 0,
    Streamer = 1,
    LivingRoom = 2,
    Custom = 3
}

public enum ProcessDisposition
{
    Allow = 0,
    Suspend = 1,
    Terminate = 2
}
