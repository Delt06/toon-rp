using UnityEngine;

namespace DELTation.ToonRP.Extensions
{
    public struct ToonPrePassRequirement
    {
        public PrePassMode Mode;
        public ToonRenderingEvent Event;

        public ToonPrePassRequirement(PrePassMode mode, ToonRenderingEvent @event)
        {
            Mode = mode;
            Event = @event;
        }

        public static readonly ToonPrePassRequirement Off = new(PrePassMode.Off, ToonRenderingEvent.InvalidLatest);

        public static ToonPrePassRequirement Combine(ToonPrePassRequirement r1, ToonPrePassRequirement r2)
        {
            if (r1.Mode == PrePassMode.Off)
            {
                return r2;
            }

            if (r2.Mode == PrePassMode.Off)
            {
                return r1;
            }

            var earliestEvent = (ToonRenderingEvent) Mathf.Min((int) r1.Event, (int) r2.Event);
            return new ToonPrePassRequirement(r1.Mode | r2.Mode, earliestEvent);
        }
    }

    public static class ToonPrePassRequirementExtensions
    {
        public static ToonPrePassRequirement Sanitize(this ToonPrePassRequirement requirement)
        {
            requirement.Mode = requirement.Mode.Sanitize();
            if (requirement.Mode == PrePassMode.Off)
            {
                requirement.Event = ToonRenderingEvent.InvalidLatest;
            }

            return requirement;
        }

        public static bool UseDepthCopy(this in ToonPrePassRequirement requirement) =>
            requirement is { Mode: PrePassMode.Depth, Event: >= ToonRenderingEvent.AfterOpaque };
    }
}