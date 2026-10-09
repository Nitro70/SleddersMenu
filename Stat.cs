using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace SleddersMenu
{
    // One editable float on the sled: a menu row with Set, Freeze and Stock.
    internal sealed class Stat
    {
        public readonly string Label;
        public readonly string Tip;
        readonly Func<Sled, object> owner;
        readonly FieldInfo fieldInfo;          // null for property-backed stats (mass)
        readonly Func<Sled, float?> getter;
        readonly Action<Sled, float> setter;
        readonly Func<float, string> extra;
        readonly string format;

        public bool Frozen;
        public float FrozenValue;
        public string Text = "";
        public bool Editing;               // the user typed in the box; stop mirroring the live value
        public float? Stock { get; private set; }   // value before this menu first touched the current sled

        Stat(string label, string tip, string format, Func<float, string> extra,
             Func<Sled, object> owner, FieldInfo field, Func<Sled, float?> getter, Action<Sled, float> setter)
        {
            Label = label; Tip = tip; this.format = format; this.extra = extra;
            this.owner = owner; this.fieldInfo = field; this.getter = getter; this.setter = setter;
        }

        static Stat OnField(string label, string tip, Func<Sled, object> owner, Type type, string name,
                            string format = "0.###", Func<float, string> extra = null)
        {
            var f = Sled.Find(type, name);
            return new Stat(label, tip, format, extra, owner, f, null, null);
        }

        public bool Available => fieldInfo != null || getter != null;

        public float? Read(Sled s)
        {
            if (!s.Valid) return null;
            if (getter != null) return getter(s);
            var o = owner(s);
            if (o == null || fieldInfo == null) return null;
            return (float)fieldInfo.GetValue(o);
        }

        public void Write(Sled s, float value)
        {
            var current = Read(s);
            if (current == null) return;
            if (Stock == null) Stock = current;
            if (setter != null) setter(s, value);
            else fieldInfo.SetValue(owner(s), value);
        }

        public void ForgetSled() { Stock = null; Editing = false; }

        public string Format(float v) => v.ToString(format, CultureInfo.InvariantCulture);
        public string Extra(float v) => extra != null ? extra(v) : "";

        public static bool TryParse(string text, out float value) =>
            float.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        // power = horsePower * 782.7273 (SnowmobileControllerBase, applied when the sled spawns).
        public const float PowerPerHp = 782.7273f;
        static string Kmh(float metresPerSecond) => (metresPerSecond * 3.6f).ToString("0", CultureInfo.InvariantCulture) + " km/h";

        public static List<Stat> All()
        {
            Func<Sled, object> mesh = s => s.Mesh;
            Func<Sled, object> ctrl = s => s.Controller;
            Func<Sled, object> bas = s => s.Base;
            var mi = typeof(MeshInterpretter);
            var sc = typeof(SnowmobileController);

            return new List<Stat>
            {
                OnField("Power", "Engine power (the 'power' field). Stock = horsepower x 782.73. Drive force = throttle x power x efficiency / speed.",
                        mesh, mi, "power", "0", v => (v / PowerPerHp).ToString("0", CultureInfo.InvariantCulture) + " hp"),
                OnField("Power efficiency", "Straight multiplier on power. Stock 1.", mesh, mi, "powerEfficiency"),
                OnField("Throttle curve", "Throttle input is raised to this power. Below 1 = more power at part throttle.", ctrl, sc, "throttleExponent"),
                OnField("Limiter start", "Track speed (m/s) where power starts fading out.", mesh, mi, "drivetrainMaxSpeed1", extra: Kmh),
                OnField("Top speed", "Track speed (m/s) where power reaches zero. Raise with Limiter start to go faster.", mesh, mi, "drivetrainMaxSpeed2", extra: Kmh),
                OnField("Launch cap", "Force is power / max(speed, this). Lower = harder launch from a stop.", mesh, mi, "drivetrainMinSpeed", extra: Kmh),
                OnField("Brake force", "Brake input x this (the 'breakForce' field).", mesh, mi, "breakForce"),
                OnField("Track drag", "Constant track friction, added on top of braking. Lower = coasts further.", mesh, mi, "frictionForce"),
                OnField("Snow grip", "Track friction coefficient against snow (from the sled's stats).", mesh, mi, "coefficientOfFriction"),
                OnField("Lug height", "Track paddle bite in snow (stock = lug mm x 0.00237).", mesh, mi, "lugHeight", "0.#####"),
                OnField("Snow flotation", "How hard the snow pushes the sled up. Higher = floats on powder.", mesh, mi, "snowSupportForceFactor"),
                OnField("Steering lock", "Maximum ski angle in degrees.", bas, typeof(SnowmobileControllerBase), "skisMaxAngle"),
                new Stat("Weight", "Rigidbody mass in kg (sled weight + 58 kg rider).", "0", null, null, null,
                         s => s.Body != null ? s.Body.mass : (float?)null,
                         (s, v) => { if (s.Body != null) s.Body.mass = Mathf.Max(1f, v); }),
            };
        }
    }
}
