using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine;


[CreateAssetMenu(order = 0, fileName = "new Dynamic Model", menuName = "Models/Dynamic")]
public class DynamicReservoirModel : ScriptableObject, ISerializationCallbackReceiver
{
    public Cartesian Grid;
    public ComputeShader ProcessDown;

    public Reservoir Reservoir;
    public Fluid[] Fluids;
    public double TimeStep;

    public double[] BMinX,
        BMaxX,
        BMinY,
        BMaxY,
        BMinZ,
        BMaxZ;

    public WellGrid[] Wells;


    public double[,] Transmissibility { get; private set; }
    public SparseMatrix Matrix { get; private set; }
    public double[] Vector { get; private set; }
    public Matrix<double> Inverse { get; private set; }
    public Array1DDouble[] Inverse_dat;

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
                    var n = Grid.Ids[dim * 2 + dir + 1][i];
                    var d = (Grid.Size[dim][i] + Grid.Size[dim][n]) / 2;
                    delta[i, dim * 2 + dir] = d;
                    var k = 2 * d / (Grid.Size[dim][i] / Reservoir.GetPermeability(i, dim) +
                                     Grid.Size[dim][n] / Reservoir.GetPermeability(n, dim));

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
                            noBorderNeighborMin[indexMin] = Grid.Ids[dim * 2 + 1][i];
                            matrixValuesMin[indexMin] = Transmissibility[i, dim * 2];
                            indexMin++;
                        }

                        if (pos[dim] != Grid.GridNum[dim] - 1)
                        {
                            noBorderMax[indexMax] = i;
                            noBorderNeighborMax[indexMax] = Grid.Ids[dim * 2 + 2][i];
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

    private void ImplementBorderConditions(
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

                var dMin = Grid.Size[dim][min];
                var dMax = Grid.Size[dim][max];

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

    private void ImplementWell(
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

    public void Prepare()
    {
        SetTransmissibility();
        BuildMatrix();
        ImplementBorderConditions(0, BMinX, BMaxX);
        ImplementBorderConditions(1, BMinY, BMaxY);
        ImplementBorderConditions(2, BMinZ, BMaxZ);

        var matrix = new SparseMatrix(Grid.Num, Grid.Num);
        Matrix.CopyTo(matrix);
        for (var i = 0; i < Grid.Num; i++)
        {
            matrix[i, i] -= 1 / TimeStep;
        }

        Inverse = matrix.Inverse();
        Debug.Log(Inverse);
        var inverser = new Inverser(matrix, (Grid.GridNum[0] , Grid.GridNum[1], Grid.GridNum[2]));
        Inverse = inverser.InverseMatrix();
        Debug.Log(Inverse);
    }

    public double[] Solve(double[] Pressure)
    {
        var b = new DenseVector(Grid.Num);
        for (var id = 0; id < Grid.Num; id++)
        {
            b[id] = -Pressure[id] / TimeStep + Vector[id];
        }

        var res = Inverse.Multiply(b);


        return res.AsArray();
    }

    [System.Serializable]
    public struct WellGrid
    {
        public int Id;
        public double ProductivityIndex;
        public double BottomHolePressure;
    }

    public void OnBeforeSerialize()
    {
        try
        {
            Inverse_dat = new Array1DDouble[Grid.Num];
            for (var x = 0; x < Grid.Num; x++)
            {
                Inverse_dat[x] = new Array1DDouble(Grid.Num);
                for (var y = 0; y < Grid.Num; y++)
                {
                    Inverse_dat[x][y] = Inverse[x, y];
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Inverse Data deleted please prepare model again");
        }
    }

    public void OnAfterDeserialize()
    {
        Inverse = new DenseMatrix(Grid.Num, Grid.Num);
        for (var x = 0; x < Grid.Num; x++)
        {
            for (var y = 0; y < Grid.Num; y++)
            {
                Inverse[x, y] = Inverse_dat[x][y];
            }
        }
    }
}