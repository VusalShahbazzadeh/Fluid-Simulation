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
    private int FinRange;

    private ComputeShader ProcessDownShader;
    private ComputeBuffer ProcessDownBufferMatrix;
    private ComputeBuffer ProcessDownBufferInverse;

    public Inverser(Matrix<double> matrix, int range, ComputeShader processDownShader)
    {
        ProcessDownShader = processDownShader;
        Num = matrix.Storage.ColumnCount;
        Matrix = new double[Num * Num];
        Inverse = new double[Num * Num];

        for (var i = 0; i < Num; i++)
        {
            Inverse[i+ i*Num] = 1;
        }
        for (var x = 0; x < Num; x++)
        {
            for (var y = 0; y < Num; y++)
            {
                Matrix[x + y * Num] = matrix[y, x];
            }
        }

        Range = range;
        FinRange = Range;
    }

    public Matrix<double> InverseMatrix()
    {
        ProcessDownBufferMatrix = new ComputeBuffer(Num*Num,sizeof(double));
        ProcessDownBufferMatrix.SetData(Matrix);
        ProcessDownShader.SetBuffer(0,"matrix_buffer",ProcessDownBufferMatrix);
        ProcessDownBufferInverse = new ComputeBuffer(Num * Num, sizeof(double));
        ProcessDownBufferInverse.SetData(Inverse);
        ProcessDownShader.SetBuffer(0,"inverse", ProcessDownBufferInverse);
        ProcessDownShader.SetInt("num",Num);

        Sort();

        for (var i = 0; i < Num; i++)
        {
            ProcessColumnDown(i);
        }

        for (var i = Num - 1; i >= 0; i--)
        {
            ProcessColumnUp(i);
        }
        
        ProcessDownBufferMatrix.Dispose();
        ProcessDownBufferInverse.Dispose();

        for (var i = 0; i < Num; i++)
        {
            DivideRow(i, Matrix[i+ i*Num]);
        }

        var result = new DenseMatrix(Num, Num);
        
        for (var row = 0; row < Num; row++)
        {
            for (var column = 0; column < Num; column++)
            {
                result[row, column] = Inverse[column + row * Num];
            }
        }

        return result;
    }

    private void Sort()
    {
        for (var i = 0; i < Num; i++)
        {
            if (Matrix[i+ i*Num] != 0)
            {
                continue;
            }

            for (var j = 1; j <= Range; j++)
            {
                if (Matrix[i+ (i + j)*Num] == 0)
                    continue;
                SwitchRows(i, (i + j)*Num);
                FinRange = Math.Max(FinRange, Range + j);
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
            res[i] = Matrix[i+ row*Num];
        }

        for (var i = 0; i < Num; i++)
        {
            res[i + Num] = Inverse[i +row*Num];
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
            Matrix[i+ row*Num] = data[i];
        }

        for (var i = 0; i < Num; i++)
        {
            Inverse[i+ row*Num] = data[i + Num];
        }
    }

    public void ProcessColumnDown(int column)
    {
        var distanceToBottom = Num - 1 - column;
        var range = Math.Min(distanceToBottom, Range);

        ProcessDownShader.SetInt("column",column);
        
        ProcessDownShader.Dispatch(0,FinRange,1,1);

        // for (var i = 1; i <= range; i++)
        // {
        //     if (Matrix[column+ (column + i)*Num] == 0)
        //         continue;
        //
        //     var _row = GetRow(column);
        //     var _multiplied = Multiply(_row, Matrix[column+ (column + i)*Num] / Matrix[column+ column*Num]);
        //     SubtractRow(column + i, _multiplied);
        // }
    }

    public void ProcessColumnUp(int column)
    {
        var distanceToTop = column;
        var range = Math.Min(distanceToTop, Range);
        for (var i = 1; i <= range; i++)
        {
            if (Matrix[column+ (column - i)*Num] == 0)
                continue;

            var _row = GetRow(column);
            var _multiplied = Multiply(_row, Matrix[column+ (column - i)*Num] / Matrix[column+ column*Num]);
            SubtractRow(column - i, _multiplied);
        }
    }
}