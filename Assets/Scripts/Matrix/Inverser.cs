using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine;

public class Inverser
{
    public double[] Matrix;
    public double[] Inverse;

    private int Num;
    private int Range;

    private ComputeShader ProcessDownShader;
    private ComputeBuffer ProcessDownBuffer;

    public Inverser(Matrix<double> matrix, int range)
    {
        Num = matrix.Storage.ColumnCount;
        Matrix = new double[Num * Num];
        Inverse = new double[Num * Num];

        for (var i = 0; i < Num; i++)
        {
            Inverse[i* i*Num] = 1;
        }
        for (var x = 0; x < Num; x++)
        {
            for (var y = 0; y < Num; y++)
            {
                Matrix[x + y * Num] = matrix[y, x];
            }
        }

        Range = range;
    }

    public Matrix<double> InverseMatrix()
    {
        ProcessDownBuffer = new ComputeBuffer(Num*Num,sizeof(double));
        ProcessDownBuffer.SetData(Matrix);
        ProcessDownShader.SetBuffer(0,"");
        Sort();

        for (var i = 0; i < Num; i++)
        {
            ProcessColumnDown(i);
        }
        ProcessDownBuffer.Dispose();

        for (var i = Num - 1; i >= 0; i--)
        {
            ProcessColumnUp(i);
        }

        for (var i = 0; i < Num; i++)
        {
            DivideRow(i, Matrix[i, i]);
        }

        return Inverse;
    }

    private void Sort()
    {
        for (var i = 0; i < Num; i++)
        {
            if (Matrix[i, i] != 0)
            {
                continue;
            }

            for (var j = 1; j <= Range; j++)
            {
                if (Matrix[i, i + j] == 0)
                    continue;
                SwitchRows(i, i + j);
                break;
            }
        }
    }

    private void SwitchRows(int row1, int row2)
    {
        var rowData1 = GetRow(row1);
        var rowData2 = GetRow(row2);
        SetRow(row2, rowData1);
        SetRow(row1, rowData2);
    }

    private double[] Multiply(double[] data, double value)
    {
        var res = new double[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            res[i] = data[i] * value;
        }

        return res;
    }

    private void DivideRow(int row, double value)
    {
        var data = GetRow(row);
        for (var i = 0; i < data.Length; i++)
        {
            data[i] /= value;
        }

        SetRow(row, data);
    }

    private double[] GetRow(int row)
    {
        var res = new double[Num * 2];

        for (int i = 0; i < Num; i++)
        {
            res[i] = Matrix[i, row];
        }

        for (var i = 0; i < Num; i++)
        {
            res[i + Num] = Inverse[i, row];
        }

        return res;
    }

    public void SubtractRow(int row, double[] data)
    {
        var rowData = GetRow(row);
        var result = new double[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            result[i] = rowData[i] - data[i];
        }

        SetRow(row, result);
    }


    public void SetRow(int row, double[] data)
    {
        for (int i = 0; i < Num; i++)
        {
            Matrix[i, row] = data[i];
        }

        for (var i = 0; i < Num; i++)
        {
            Inverse[i, row] = data[i + Num];
        }
    }

    public void ProcessColumnDown(int column)
    {
        var distanceToBottom = Num - 1 - column;
        var range = Math.Min(distanceToBottom, Range);

        for (var i = 1; i <= range; i++)
        {
            if (Matrix[column, column + i] == 0)
                continue;

            var _row = GetRow(column);
            var _multiplied = Multiply(_row, Matrix[column, column + i] / Matrix[column, column]);
            SubtractRow(column + i, _multiplied);
        }
    }

    public void ProcessColumnUp(int column)
    {
        var distanceToTop = column;
        var range = Math.Min(distanceToTop, Range);
        for (var i = 1; i <= range; i++)
        {
            if (Matrix[column, column - i] == 0)
                continue;

            var _row = GetRow(column);
            var _multiplied = Multiply(_row, Matrix[column, column - i] / Matrix[column, column]);
            SubtractRow(column - i, _multiplied);
        }
    }
}