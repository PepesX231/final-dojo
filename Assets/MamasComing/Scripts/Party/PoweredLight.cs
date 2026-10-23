using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// Room light that dies when the power is out.
    public class PoweredLight : MonoBehaviour
    {
        Light l; float baseI;
        void Awake() { l = GetComponent<Light>(); if (l) baseI = l.intensity; }
        void Update() { if (l) l.intensity = Mathf.MoveTowards(l.intensity, PowerGrid.On ? baseI : 0f, Time.deltaTime * baseI * (PowerGrid.On ? 2f : 6f)); }
    }
}
