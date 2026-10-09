using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SleddersMenu.Mod), "SleddersMenu", "1.0.0", "Nitro70")]
[assembly: MelonGame("Hanki Games", "Sledders")]

namespace SleddersMenu
{
    public sealed class Mod : MelonMod
    {
        const float ScanInterval = 1f;

        MelonPreferences_Entry<KeyCode> toggleKey;
        readonly Sled sled = new Sled();
        readonly Trees trees = new Trees();
        List<Stat> stats;
        float nextScan;

        bool open, keyHeld;
        Rect window = new Rect(20f, 20f, 640f, 10f);
        CursorLockMode savedLock;
        bool savedVisible;

        bool infiniteFuel;
        bool? stockFuelConsumption;

        public override void OnInitializeMelon()
        {
            var cat = MelonPreferences.CreateCategory("SleddersMenu");
            toggleKey = cat.CreateEntry("MenuKey", KeyCode.F1, "Key that opens and closes the menu");
            stats = Stat.All();
            LoggerInstance.Msg($"Loaded. Press {toggleKey.Value} in game to open the menu.");
        }

        public override void OnUpdate()
        {
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + ScanInterval;
                if (sled.Refresh()) OnSledChanged();
                trees.Apply();
            }
            ApplyFreezes();
        }

        // Freezes are re-applied before physics too, so a value the game resets on respawn
        // is back before the next physics step.
        public override void OnFixedUpdate() => ApplyFreezes();

        public override void OnLateUpdate()
        {
            if (!open) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnSledChanged()
        {
            foreach (var st in stats) st.ForgetSled();
            stockFuelConsumption = null;
            if (sled.Valid) LoggerInstance.Msg($"Found your sled: {sled.Name}");
        }

        void ApplyFreezes()
        {
            if (!sled.Valid) return;
            foreach (var st in stats)
                if (st.Frozen) st.Write(sled, st.FrozenValue);
            if (infiniteFuel && sled.FuelControlsAvailable)
            {
                if (stockFuelConsumption == null) stockFuelConsumption = sled.FuelConsumption;
                sled.FuelConsumption = false;
            }
        }

        // Key handling goes through IMGUI events so it works whatever input backend the game uses.
        public override void OnGUI()
        {
            var e = Event.current;
            if (e.isKey && e.keyCode == toggleKey.Value)
            {
                if (e.type == EventType.KeyDown && !keyHeld) { keyHeld = true; SetOpen(!open); e.Use(); }
                else if (e.type == EventType.KeyUp) keyHeld = false;
            }
            if (!open) return;
            window = GUILayout.Window(0x51ED, window, DrawWindow, "Sledders Menu", GUILayout.Width(640f));
        }

        void SetOpen(bool value)
        {
            if (value == open) return;
            open = value;
            if (open)
            {
                savedLock = Cursor.lockState;
                savedVisible = Cursor.visible;
            }
            else
            {
                Cursor.lockState = savedLock;
                Cursor.visible = savedVisible;
            }
        }

        void DrawWindow(int id)
        {
            if (!sled.Valid)
            {
                GUILayout.Label("No sled found. Get on your snowmobile; the menu checks every second.");
            }
            else
            {
                var hp = sled.StockHorsePower;
                GUILayout.Label($"Sled: {sled.Name}    Speed: {sled.SpeedKmh:0} km/h    Stock: {(hp.HasValue ? hp.Value.ToString("0") + " hp" : "?")}");

                GUILayout.BeginHorizontal();
                GUILayout.Label("Value", GUILayout.Width(120f));
                GUILayout.Label("Live", GUILayout.Width(80f));
                GUILayout.Label("", GUILayout.Width(70f));
                GUILayout.Label("New value", GUILayout.Width(90f));
                GUILayout.EndHorizontal();

                foreach (var st in stats) DrawRow(st);

                GUILayout.Space(6f);
                GUILayout.BeginHorizontal();
                GUI.enabled = sled.FuelControlsAvailable;
                if (GUILayout.Button("Refill fuel", GUILayout.Width(100f))) sled.Refuel();
                bool fuel = GUILayout.Toggle(infiniteFuel, "Infinite fuel");
                if (fuel != infiniteFuel) SetInfiniteFuel(fuel);
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Unfreeze all", GUILayout.Width(100f)))
                    foreach (var st in stats) st.Frozen = false;
                GUILayout.EndHorizontal();
            }

            bool showTrees = GUILayout.Toggle(!trees.Hidden, new GUIContent("Show trees",
                "Off hides every tree and its collider, so you drive straight through. Stays off across levels until you tick it again."));
            if (showTrees == trees.Hidden) trees.SetHidden(!showTrees);

            GUILayout.Label(string.IsNullOrEmpty(GUI.tooltip) ? $"Hover a name for what it does.  {toggleKey.Value} closes." : GUI.tooltip,
                            GUILayout.Height(36f));
            GUI.DragWindow();
        }

        void DrawRow(Stat st)
        {
            var live = st.Read(sled);
            if (live.HasValue && !st.Editing) st.Text = st.Format(live.Value);

            GUILayout.BeginHorizontal();
            GUI.enabled = st.Available && live.HasValue;
            GUILayout.Label(new GUIContent(st.Label, st.Tip), GUILayout.Width(120f));
            GUILayout.Label(live.HasValue ? st.Format(live.Value) : "n/a", GUILayout.Width(80f));
            GUILayout.Label(live.HasValue ? st.Extra(live.Value) : "", GUILayout.Width(70f));

            string typed = GUILayout.TextField(st.Text, GUILayout.Width(90f));
            if (typed != st.Text) { st.Text = typed; st.Editing = true; }

            if (GUILayout.Button("Set", GUILayout.Width(45f)) && Stat.TryParse(st.Text, out float v))
            {
                st.Write(sled, v);
                if (st.Frozen) st.FrozenValue = v;
                st.Editing = false;
            }

            // Freezing locks in what is in the box: the live value unless you typed a new one.
            bool frozen = GUILayout.Toggle(st.Frozen, "Freeze", GUILayout.Width(65f));
            if (frozen && !st.Frozen && live.HasValue)
            {
                st.FrozenValue = Stat.TryParse(st.Text, out float f) ? f : live.Value;
                st.Write(sled, st.FrozenValue);
                st.Frozen = true;
                st.Editing = false;
            }
            else if (!frozen) st.Frozen = false;

            GUI.enabled = GUI.enabled && st.Stock.HasValue;
            if (GUILayout.Button(new GUIContent("Stock", st.Stock.HasValue ? "Back to " + st.Format(st.Stock.Value) : ""), GUILayout.Width(55f)))
            {
                st.Frozen = false;
                st.Write(sled, st.Stock.Value);
                st.Editing = false;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        void SetInfiniteFuel(bool on)
        {
            infiniteFuel = on;
            if (on || !sled.Valid) return;
            if (stockFuelConsumption.HasValue) sled.FuelConsumption = stockFuelConsumption.Value;
            stockFuelConsumption = null;
        }
    }
}
