
using UnityEngine;

[CreateAssetMenu(order =  0, fileName = "new homogeneous reservoir", menuName =  "Reservoir/Homogeneous")]
public  class HomogenousReservoir : Reservoir
{
    public double[] Permeability;
    public double Porosity;

    public override double GetPermeability(int id, int dim)
    {
        return Permeability[dim];
    }

    public override double GetPorosity(int id)
    {
        return Porosity;
    }
}
