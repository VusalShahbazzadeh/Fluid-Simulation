using UnityEngine;

[CreateAssetMenu(order =  0, fileName = "new fluid", menuName = "Fluid")]
public class Fluid : ScriptableObject
{
    public double Viscosity;
    public double Compressibility;
}
