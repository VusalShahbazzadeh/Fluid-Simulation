
using UnityEngine;


public abstract class Reservoir : ScriptableObject
{
    public abstract double GetPermeability(int id, int dim);
    public abstract double GetPorosity(int id);
}
