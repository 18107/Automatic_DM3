using UnityEngine;
using UnityModManagerNet;

namespace Automatic_DM3
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("CVT", Tooltip = "Continously Variable Transmission - Continuously change gears instead of in steps. Don't think too hard about the gearbox.")]
        public bool CVT = false;
        private bool lastCVT;
        internal bool CVTActive;

        public bool IVT = false;

        public float IVTMin = 40;
        public float IVTMax = 0.0001f;
        private string min;
        private string max;

        [Draw("Max RPM", VisibleOn = "CVT|false", Tooltip = "The RPM to shift up a gear. Redline is 1000. Default 900.")]
        public float maxRPM = 900;

        [Draw("Downshift offset", VisibleOn = "CVT|false", Tooltip = "How much below Max RPM should the engine be after downshifting. Default 50.")]
        public float RPMoffset = 50;

        [Draw("Delay while shifting", VisibleOn = "CVT|false", Tooltip = "Seconds between reducing throttle and shifting gear. Default 0.5")]
        public float shiftDelay = 0.5f;

        [Draw("Delay after shifting", VisibleOn = "CVT|false", Tooltip = "Seconds to wait after shifting gear before shifting gear again. Default 1.")]
        public float postShiftDelay = 1;

        [Draw("Gear skip time", VisibleOn = "CVT|false", Tooltip = "If the RPM reaches the target in less than this time, skip one gear. Default 2.")]
        public float doubleShiftTime = 2;

        private readonly System.Timers.Timer timer = new System.Timers.Timer(1000) { AutoReset = false, Enabled = false };

        internal void PostLoad()
        {
            lastCVT = CVT;
            CVTActive = CVT;

            min = IVTMin.ToString();
            max = IVTMax.ToString();

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
                Main.DM3s.SwitchGearboxType(CVT);
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

        public void OnGui(UnityModManager.ModEntry modEntry)
        {
            this.Draw(modEntry);

            if (CVT)
            {
                string tempS;
                float tempF;
                GUILayout.BeginHorizontal();
                GUILayout.Label("IVT");
                GUIHelper.DrawTooltip("Infinitely Variable Transmission - Effectively infinite gear range. Definitely cheating.");
                IVT = GUILayout.Toggle(IVT, "");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (IVT)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("IVT limits");
                    GUIHelper.DrawTooltip("The gear ratio (X:1) that the gearbox can't exceed. Standard gearbox 20, 3. Defaults 40, 0.0001.");
                    GUILayout.Label("Min");
                    tempS = GUILayout.TextField(min, GUILayout.MinWidth(70));
                    if (tempS != min && float.TryParse(tempS, out tempF))
                    {
                        IVTMin = tempF;
                        min = tempS;
                    }
                    GUILayout.Label(" Max");
                    tempS = GUILayout.TextField(max, GUILayout.MinWidth(70));
                    if (tempS != max && float.TryParse(tempS, out tempF))
                    {
                        IVTMax = tempF;
                        max = tempS;
                    }
                    if (IVTMin != 40 || IVTMax != 0.0001f)
                    {
                        if (GUILayout.Button("Reset"))
                        {
                            IVTMin = 40;
                            IVTMax = float.Epsilon;
                            min = IVTMin.ToString();
                            max = IVTMax.ToString();
                        }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}
