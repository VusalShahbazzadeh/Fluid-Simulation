using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine;

public class DynamicReservoirModel
{
    public Cartesian Grid;
    public Reservoir Reservoir { get; private set; }
    public Fluid[] Fluids { get; private set; }
    public double TimeTotal { get; private set; }
    public double TimeStep { get; private set; }
    public int FrameCount { get; private set; }
    public double[,] Pressure { get; private set; }

    public double[,] Transmissibility { get; private set; }

    public SparseMatrix Matrix { get; private set; }

    public double[] Vector { get; private set; }

    public DynamicReservoirModel(
        (float x, float y, float z) length,
        (int x, int y, int z) gridNum,
        Reservoir reservoir,
        Fluid[] fluids,
        double timeTotal,
        double timeStep,
        double[] initialPressure)
    {
        Grid = new Cartesian(length, gridNum);
        Reservoir = reservoir;
        Fluids = fluids;
        TimeTotal = timeTotal;
        TimeStep = timeStep;
        FrameCount = (int) (TimeTotal / TimeStep) + 1;
        Pressure = new double[Grid.Num, FrameCount];
        for (var i = 0; i < Grid.Num; i++)
        {
            Pressure[i, 0] = initialPressure[i];
        }

        SetTransmissibility();
        BuildMatrix();
    }

    private void SetTransmissibility()
    {
        Transmissibility = new double[Grid.Num, 6];

        var delta = new double[Grid.Num, 6];
        var Eta = new double[Grid.Num, 6];
        for (var dim = 0; dim < 3; dim++)
        {
            for (var dir = 0; dir < 2; dir++)
            {
                for (var i = 0; i < Grid.Num; i++)
                {
                    var n = Grid.Ids[i, dim * 2 + dir + 1];
                    var d = (Grid.Size[i, dim] + Grid.Size[n, dim]) / 2;
                    delta[i, dim * 2 + dir] = d;
                    var k = 2 * d / (Grid.Size[i, dim] / Reservoir.GetPermeability(i, dim) +
                                     Grid.Size[n, dim] / Reservoir.GetPermeability(n, dim));

                    Eta[i, dim * 2 + dir] =
                        k / Reservoir.GetPorosity(i) / Fluids[0].Viscosity / Fluids[0].Compressibility;
                }
            }
        }

        for (var dim = 0; dim < 3; dim++)
        {
            for (var dir = 0; dir < 2; dir++)
            {
                for (var i = 0; i < Grid.Num; i++)
                {
                    Transmissibility[i, dim * 2 + dir] = (2 * Eta[i, dim * 2 + dir])
                                                         /
                                                         (delta[i, dim * 2 + dir] *
                                                          (delta[i, dim * 2] + delta[i, dim * 2 + 1]));
                }
            }
        }
    }

    private void BuildMatrix()
    {
        Matrix = new SparseMatrix(Grid.Num, Grid.Num);

        for (var dim = 0; dim < 3; dim++)
        {
            var count =
                Grid.GridNum[0] * Grid.GridNum[1] * Grid.GridNum[2]
                -
                Grid.GridNum[0] * Grid.GridNum[1] * Grid.GridNum[2]
                /
                Grid.GridNum[dim];


            var noBorderMin = new int[count];
            var noBorderMax = new int[count];
            var noBorderNeighborMin = new int[count];
            var noBorderNeighborMax = new int[count];
            var matrixValuesMin = new double[count];
            var matrixValuesMax = new double[count];
            var pos = new[]
            {
                0, 0, 0
            };

            int indexMin = 0;
            int indexMax = 0;
            for (pos[2] = 0; pos[2] < Grid.GridNum[2]; pos[2]++)
            {
                for (pos[1] = 0; pos[1] < Grid.GridNum[1]; pos[1]++)
                {
                    for (pos[0] = 0; pos[0] < Grid.GridNum[0]; pos[0]++)
                    {
                        int i = pos[0]
                                +
                                pos[1] * Grid.GridNum[0]
                                +
                                pos[2] * Grid.GridNum[0] * Grid.GridNum[1];
                        if (pos[dim] != 0)
                        {
                            noBorderMin[indexMin] = i;
                            noBorderNeighborMin[indexMin] = Grid.Ids[i, dim * 2 + 1];
                            matrixValuesMin[indexMin] = Transmissibility[i, dim * 2];
                            indexMin++;
                        }

                        if (pos[dim] != Grid.GridNum[dim] - 1)
                        {
                            noBorderMax[indexMax] = i;
                            noBorderNeighborMax[indexMax] = Grid.Ids[i, dim * 2 + 2];
                            matrixValuesMax[indexMax] = Transmissibility[i, dim * 2 + 1];
                            indexMax++;
                        }
                    }
                }
            }

            for (var i = 0; i < count; i++)
            {
                Matrix[noBorderMin[i], noBorderNeighborMin[i]] += matrixValuesMin[i];
                Matrix[noBorderMin[i], noBorderMin[i]] -= matrixValuesMin[i];

                Matrix[noBorderMax[i], noBorderNeighborMax[i]] += matrixValuesMax[i];
                Matrix[noBorderMax[i], noBorderMax[i]] -= matrixValuesMax[i];
            }
        }

        Vector = new double[Grid.Num];
    }

    public void ImplementBorderConditions(
        int dim,
        double[] bMin = null,
        double[] bMax = null
    )
    {
        bMin ??= new double[] {0, 1, 0};
        bMax ??= new double[] {0, 1, 0};


        if (Grid.GridNum[dim] <= 1) return;

        var dim1 = (dim + 1) % 3;
        var dim2 = (dim - 1) % 3;
        dim2 += dim2 < 0 ? 3 : 0;

        var pos = new int[]
        {
            0, 0, 0
        };
        for (pos[dim1] = 0; pos[dim1] < Grid.GridNum[dim1]; pos[dim1]++)
        {
            for (pos[dim2] = 0; pos[dim2] < Grid.GridNum[dim2]; pos[dim2]++)
            {
                var p = 1;

                for (var w = dim - 1; w >= 0; w--)
                {
                    p *= Grid.GridNum[w];
                }

                int min = pos[0]
                          +
                          pos[1] * Grid.GridNum[0]
                          +
                          pos[2] * Grid.GridNum[0] * Grid.GridNum[1]
                          -
                          pos[dim] * p;

                int max = min
                          +
                          (Grid.GridNum[dim] - 1) * p;

                var dMin = Grid.Size[min, dim];
                var dMax = Grid.Size[max, dim];

                var tMin = Transmissibility[min, 2 * dim];
                var tMax = Transmissibility[max, 2 * dim + 1];

                var bcMin = 2 * tMin * dMin / (bMin[0] * dMin - 2 * bMin[1]);
                var bcMax = 2 * tMax * dMax / (bMax[0] * dMax + 2 * bMax[1]);

                Matrix[min, min] -= bcMin * bMin[0];
                Matrix[max, max] -= bcMax * bMax[0];

                Vector[min] -= bcMin * bMin[2];
                Vector[max] -= bcMax * bMax[2];
            }
        }
    }

    public void ImplementWell(
        int[] ids,
        double[] productivityIndexes,
        double[] bottomHolePressure
    )
    {
        for (var i = 0; i < ids.Length; i++)
        {
            Vector[ids[i]] += productivityIndexes[i] * bottomHolePressure[i];
            Matrix[ids[i], ids[i]] += productivityIndexes[i];
        }
    }

    public void Solve()
    {
        var matrix = new SparseMatrix(Grid.Num, Grid.Num);
        Matrix.CopyTo(matrix);
        for (var i = 0; i < Grid.Num; i++)
        {
            matrix[i, i] -= 1 / TimeStep;
        }

        for (var i = 1; i < FrameCount; i++)
        {
            var b = new DenseVector(Grid.Num);
            for (var id = 0; id < Grid.Num; id++)
            {
                b[id] = -Pressure[id, i - 1] / TimeStep + Vector[id];
            }

            var res = matrix.Solve(b);

            for (var id = 0; id < res.Count; id++)
            {
                Pressure[id, i] = res[id];
            }
        }
    }
}