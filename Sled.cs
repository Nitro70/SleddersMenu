using System;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace SleddersMenu
{
    // The local player's sled. Remote players drive SnowmobileControllerRemote, so the only
    // active SnowmobileController in the scene is ours.
    //
    // Everything is looked up by field name through reflection. Sledders is obfuscated, but
    // Unity-serialized fields (power, breakForce, controllerBase, ...) keep their real names,
    // and a field that disappears in an update only disables its own row instead of the menu.
    internal sealed class Sled
    {
        internal const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static readonly FieldInfo controllerBaseField = Find(typeof(SnowmobileController), "controllerBase");
        static readonly FieldInfo meshField = Find(typeof(SnowmobileControllerBase), "meshInterpretter");
        static readonly FieldInfo bodyField = Find(typeof(SnowmobileControllerBase), "mainBody");
        static readonly FieldInfo vehicleField = FindByType(typeof(SnowmobileController), typeof(VehicleScriptableObject));
        static readonly FieldInfo fuelFlagField = Find(typeof(SnowmobileController), "enableFuelConsumption");
        static readonly MethodInfo setFuelMethod = typeof(SnowmobileController).GetMethod("SetFuel", AnyInstance, null, new[] { typeof(float) }, null);

        public SnowmobileController Controller { get; private set; }
        public SnowmobileControllerBase Base { get; private set; }
        public MeshInterpretter Mesh { get; private set; }
        public Rigidbody Body { get; private set; }
        public VehicleScriptableObject Vehicle { get; private set; }

        // Unity's == also catches a destroyed object (respawn, sled swap, back to menu).
        public bool Valid => Controller != null && Mesh != null;

        // Returns true when the sled we point at changed (a different instance, or gone).
        public bool Refresh()
        {
            SnowmobileController found = null;
            foreach (var c in UnityEngine.Object.FindObjectsByType<SnowmobileController>(FindObjectsSortMode.None))
            {
                if (c != null && c.isActiveAndEnabled) { found = c; break; }
            }
            // Keep a sled that is only briefly disabled, so its stock values survive.
            if (found == null && Controller != null) found = Controller;
            if (ReferenceEquals(found, Controller)) return false;

            Controller = found;
            Base = found != null ? controllerBaseField?.GetValue(found) as SnowmobileControllerBase : null;
            Mesh = Base != null ? meshField?.GetValue(Base) as MeshInterpretter : null;
            Body = Base != null ? bodyField?.GetValue(Base) as Rigidbody : null;
            Vehicle = found != null ? vehicleField?.GetValue(found) as VehicleScriptableObject : null;
            return true;
        }

        public string Name => Vehicle != null ? Vehicle.displayName : "?";
        public float? StockHorsePower => Vehicle != null ? Vehicle.horsePower : (float?)null;
        public float SpeedKmh => Body != null ? Body.linearVelocity.magnitude * 3.6f : 0f;

        public bool FuelControlsAvailable => fuelFlagField != null && setFuelMethod != null;

        public bool FuelConsumption
        {
            get => fuelFlagField != null && Controller != null && (bool)fuelFlagField.GetValue(Controller);
            set { if (fuelFlagField != null && Controller != null) fuelFlagField.SetValue(Controller, value); }
        }

        // Fuel is stored as a 0..1 fraction of the tank.
        public void Refuel() => setFuelMethod?.Invoke(Controller, new object[] { 1f });

        internal static FieldInfo Find(Type type, string name)
        {
            var f = type.GetField(name, AnyInstance);
            if (f == null) MelonLogger.Warning($"{type.Name}.{name} not found, its row is disabled (game update?)");
            return f;
        }

        static FieldInfo FindByType(Type owner, Type fieldType)
        {
            foreach (var f in owner.GetFields(AnyInstance))
                if (f.FieldType == fieldType) return f;
            MelonLogger.Warning($"No {fieldType.Name} field on {owner.Name}, sled name and stock HP hidden");
            return null;
        }
    }
}
