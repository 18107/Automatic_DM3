using UnityModManagerNet;

namespace Automatic_DM3
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Max RPM", Tooltip = "The RPM to shift up a gear. Redline is 1000. Default 900")]
        public float maxRPM = 900;

        [Draw("Min RPM", Tooltip = "The RPM to shift down a gear. Idle is 300. Default 600")]
        public float minRPM = 600;

        [Draw("Delay while shifting", Tooltip = "Seconds between reducing throttle and shifting gear.")]
        public float shiftDelay = 0;

        [Draw("Delay after shifting", Tooltip = "Seconds to wait after shifting gear before shifting gear again. Recommended above 0.5")]
        public float postShiftDelay = 0.5f;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
            
        }
    }
}
