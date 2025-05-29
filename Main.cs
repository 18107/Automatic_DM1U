using HarmonyLib;
using System;
using System.Reflection;
using UnityModManagerNet;

namespace Automatic_DM1U
{
    public class Main
    {
        public static UnityModManager.ModEntry mod { get; private set; }

        internal static Settings settings { get; private set; }

        internal static readonly DM1UList DM1Us = new DM1UList();

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            Harmony harmony = null;
            try
            {
                harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", e);
                harmony?.UnpatchAll();
                return false;
            }

            //Setup GUI
            settings = Settings.Load<Settings>(modEntry);
            settings.PostLoad();
            modEntry.OnGUI += settings.Draw;
            modEntry.OnSaveGUI += settings.Save;
            modEntry.OnFixedUpdate += DM1Us.FixedUpdate;
            modEntry.OnToggle += (mod, value) =>
            {
                if (value)
                {
                    //Find any DM1Us not already registered
                    DM1Us.AddAll();
                }
                else
                {
                    DM1Us.ForEach(s => s.OnEndControl());
                }
                //Mod toggled sucessfully
                return true;
            };

            return true; //Loaded successfully
        }
    }
}
