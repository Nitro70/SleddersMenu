using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace SleddersMenu
{
    // All trees go through one TreeRenderer object: it GPU-instances the visible trees and spawns
    // the hittable tree colliders near the player as its own children. Turning that object off
    // hides both, the same as disabling it in UnityExplorer. The game empties the Unity terrain
    // trees, so there is nothing else to hide.
    internal sealed class Trees
    {
        readonly HashSet<GameObject> disabledByUs = new HashSet<GameObject>();

        public bool Hidden { get; private set; }

        public void SetHidden(bool hide)
        {
            Hidden = hide;
            if (hide) Apply();
            else Restore();
        }

        // Called on every scan too, because loading a level brings a new TreeRenderer.
        public void Apply()
        {
            if (!Hidden) return;
            foreach (var tr in Object.FindObjectsByType<TreeRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var go = tr.gameObject;
                if (!go.activeSelf) continue;
                go.SetActive(false);
                if (disabledByUs.Add(go)) MelonLogger.Msg($"Trees hidden (disabled '{go.name}')");
            }
        }

        // Only turns back on what this menu turned off.
        void Restore()
        {
            foreach (var go in disabledByUs)
                if (go != null) go.SetActive(true);
            disabledByUs.Clear();
        }
    }
}
