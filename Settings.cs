using UnityModManagerNet;

namespace Automatic_DM1U
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("CVT", Tooltip = "Continously Variable Transmission - Continuously change gears instead of in steps. Don't think too hard about the gearbox.")]
        public bool CVT = false;
        private bool lastCVT;
        internal bool CVTActive;

        [Draw("IVT", VisibleOn = "CVT|true", Tooltip = "Infinitely Variable Transmission - Effectively infinite gear range. Definitely cheating.")]
        public bool IVT = false;

        [Draw("Target RPM", VisibleOn = "CVT|true", Tooltip = "What RPM should the CVT try to keep. Default 1900.")]
        public float targetRPM = 1900;

        [Draw("Max RPM", VisibleOn = "CVT|false", Tooltip = "The RPM to shift up a gear. Redline is 2200. Default 2000.")]
        public float maxRPM = 2000;

        [Draw("Downshift offset", VisibleOn = "CVT|false", Tooltip = "How much below Max RPM should the engine be after downshifting. 100 for performance, 700 to avoid wheelslip.")]
        public float RPMoffset = 700;

        [Draw("Delay while shifting", VisibleOn = "CVT|false", Tooltip = "Seconds between reducing throttle and shifting gear. Default 0.5")]
        public float shiftDelay = 0.5f;

        [Draw("Delay after shifting", VisibleOn = "CVT|false", Tooltip = "Seconds to wait after shifting gear before shifting gear again. Default 1.")]
        public float postShiftDelay = 1;

        private readonly System.Timers.Timer timer = new System.Timers.Timer(1000) { AutoReset = false, Enabled = false };

        internal void PostLoad()
        {
            lastCVT = CVT;
            CVTActive = CVT;

            //Wait for gearshift to finish before turning off CVT to prevent engine explosions
            timer.Elapsed += (_, __) => CVTActive = false;
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
            if (CVT != lastCVT)
            {
                lastCVT = CVT;
                Main.DM1Us.SwitchGearboxType(CVT);
                if (CVT)
                {
                    //Turn on CVT immediately
                    if (timer.Enabled) timer.Stop();
                    CVTActive = true;
                }
                else
                {
                    //Wait 1 second for gearshifting to finish before turning off CVT
                    timer.Start();
                }
            }
        }
    }
}
