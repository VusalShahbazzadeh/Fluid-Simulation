using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(order =  0, fileName = "new Grid", menuName = "Grid/Cartesian")]
public class Cartesian : ScriptableObject
{
    public double[] Length;
    public int[] GridNum;

    public int Num;
    public Array1DInt[] Ids;
    public Array1DDouble[] Size;
    public Array1DDouble[] Area;
    public double[] Volume;
    public Array1DDouble[] Center;

    public void Init()
    {

        Num = GridNum[0] * GridNum[1] * GridNum[2];

        Ids = new []
        {
            new Array1DInt(Num),
            new Array1DInt(Num),
            new Array1DInt(Num),
            new Array1DInt(Num),
            new Array1DInt(Num),
            new Array1DInt(Num),
            new Array1DInt(Num)
        };


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
                        Ids[w][i] = i;
                    }

                    for (int w = 0; w < dimension.Length; w++)
                    {
                        var p = 1;
                        for (int t = w - 1; t >= 0; t--)
                            p *= GridNum[t];

                        if (dimension[w] != 0)
                            Ids[w * 2 + 1][i] -= p;

                        if (dimension[w] != GridNum[w] - 1)
                            Ids[w * 2 + 2][i] += p;
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

        Size = new[]
        {
            new Array1DDouble(Num),
            new Array1DDouble(Num),
            new Array1DDouble(Num)
        };


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
                        Size[d][i] = size[d][pos[d]];
                    }
                }
            }
        }


        Area = new[]
        {
            new Array1DDouble(Num),
            new Array1DDouble(Num),
            new Array1DDouble(Num)
        };

        Volume = new double[Num];

        for (var d = 0; d < 3; d++)
        {
            for (var i = 0; i < Num; i++)
            {
                Volume[i] = 1;
                for (var d2 = 0; d2 < 3; d2++)
                {
                    Volume[i] *= Size[d2][i];
                }

                Area[d][i] = Volume[i] / Size[d][i];
            }
        }
    }
}