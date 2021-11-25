using System.Collections.Generic;
using UnityEngine;

public class Cartesian
{
    public double[] Length { get; private set; }
    public int[] GridNum { get; private set; }

    public int Num { get; private set; }
    public int[,] Ids { get; private set; }
    public double[,] Size { get; private set; }
    public double[,] Area { get; private set; }
    public double[] Volume { get; private set; }
    public double[,] Center { get; private set; }

    public Cartesian((double x, double y, double z) length, (int x, int y, int z) gridNum)
    {
        Length = new[]
        {
            length.x,
            length.y,
            length.z
        };
        GridNum = new[]
        {
            gridNum.x,
            gridNum.y,
            gridNum.z
        };

        Num = GridNum[0] * GridNum[1] * GridNum[2];


        Ids = new int[Num, 7];


        for (var z = 0; z < GridNum[2]; z++)
        {
            for (var y = 0; y < GridNum[1]; y++)
            {
                for (int x = 0; x < GridNum[0]; x++)
                {
                    var dimension = new int[]
                    {
                        x, y, z
                    };
                    int i = x + y * GridNum[0] + z * GridNum[0] * GridNum[1];
                    for (var w = 0; w < 7; w++)
                    {
                        Ids[i, w] = i;
                    }

                    for (int w = 0; w < dimension.Length; w++)
                    {
                        var p = 1;
                        for (int t = w - 1; t >= 0; t--)
                            p *= GridNum[t];

                        if (dimension[w] != 0)
                            Ids[i, w * 2 + 1] -= p;

                        if (dimension[w] != GridNum[w] - 1)
                            Ids[i, w * 2 + 2] += p;
                    }
                }
            }
        }


        var node = new List<double[]>();

        for (var d = 0; d < 3; d++)
        {
            node.Add(new double[GridNum[d] + 1]);
            for (var i = 0; i < GridNum[d] + 1; i++)
            {
                node[d][i] = Length[d] * i / GridNum[d];
            }
        }


        var size = new List<double[]>();

        for (var d = 0; d < 3; d++)
        {
            size.Add(new double[GridNum[d]]);
            for (var i = 0; i < GridNum[d]; i++)
            {
                size[d][i] = node[d][i + 1] - node[d][i];
            }
        }
        
        Size = new double[Num, 3];


        for (var d = 0; d < 3; d++)
        {
            for (var z = 0; z < GridNum[2]; z++)
            {
                for (var y = 0; y < GridNum[1]; y++)
                {
                    for (var x = 0; x < GridNum[0]; x++)
                    {
                        int[] pos =
                        {
                            x, y, z
                        };

                        int i = x + y * GridNum[0] + z * GridNum[0] * GridNum[1];
                        Size[i, d] = size[d][pos[d]];
                    }
                }
            }
        }
        
        


        Area = new double[Num, 3];
        Volume = new double[Num];

        for (var d = 0; d < 3; d++)
        {
            for (var i = 0; i < Num; i++)
            {
                Volume[i] = 1;
                for (var d2 = 0; d2 < 3; d2++)
                {
                    Volume[i] *= Size[i, d2];
                }

                Area[i, d] = Volume[i] / Size[i, d];
            }
        }
    }
}