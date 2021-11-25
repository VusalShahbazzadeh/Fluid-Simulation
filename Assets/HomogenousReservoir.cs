
public  class HomogenousReservoir : Reservoir
{
    public double[] Permeability;
    public double Porosity;
    public HomogenousReservoir(double porosity, double[] permeability)
    {
        Permeability = permeability;
        Porosity = porosity;
    }

    public override double GetPermeability(int id, int dim)
    {
        return Permeability[dim];
    }

    public override double GetPorosity(int id)
    {
        return Porosity;
    }
}
