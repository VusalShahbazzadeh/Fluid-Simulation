
public class Fluid
{
    public double Viscosity { get; private set; }
    public double Compressibility { get; private set; }


    public Fluid(double viscosity, double compressibility)
    {
        Viscosity = viscosity;
        Compressibility = compressibility;
    }
}
