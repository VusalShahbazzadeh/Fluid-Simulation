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

    private ComputeShader ProcessInverseLine;
    private ComputeBuffer inverseBuffer;
    private ComputeBuffer matrixBuffer;

    public Inverser(Matrix<double> matrix, (int x, int y, int z) size)
    {
        ProcessInverseLine = Resources.Load<ComputeShader>("MultiplyAndSubtractInverse");
        Num = matrix.Storage.ColumnCount;
        Matrix = new double[Num * Num];
        Inverse = new double[Num * Num];

        Range = 1;
        if (size.z > 1)
        {
            Range *= size.y;
        }

        if (size.y > 1)
        {
            Range *= size.x;
        }

        for (var i = 0; i < Num; i++)
        {
            Inverse[i + i * Num] = 1;
        }

        for (var x = 0; x < Num; x++)
        {
            for (var y = 0; y < Num; y++)
            {
                Matrix[x + y * Num] = matrix[y, x];
            }
        }

        inverseBuffer = new ComputeBuffer(Num * Num, sizeof(double));
        inverseBuffer.SetData(Inverse);
        matrixBuffer = new ComputeBuffer(Num * Num, sizeof(double));
        matrixBuffer.SetData(Matrix);
        ProcessInverseLine.SetBuffer(0, "inverse", inverseBuffer);
        ProcessInverseLine.SetBuffer(1, "inverse", inverseBuffer);
        ProcessInverseLine.SetBuffer(2, "inverse", inverseBuffer);
        ProcessInverseLine.SetBuffer(3, "inverse", inverseBuffer);
        ProcessInverseLine.SetBuffer(0, "matrix_buffer", matrixBuffer);
        ProcessInverseLine.SetBuffer(1, "matrix_buffer", matrixBuffer);
        ProcessInverseLine.SetBuffer(2, "matrix_buffer", matrixBuffer);
        ProcessInverseLine.SetBuffer(3, "matrix_buffer", matrixBuffer);
        ProcessInverseLine.SetInt("num", Num);
    }

    public Matrix<double> InverseMatrix()
    {
        for (var i = 0; i < Num-1; i++)
        {
            ProcessColumnDown(i);
        }

        for (var i = Num - 1; i > 0; i--)
        {
            ProcessColumnUp(i);
        }

        // var r = new DenseMatrix(Num, Num);
        //
        // for (var row = 0; row < Num; row++)
        // {
        //     for (var column = 0; column < Num; column++)
        //     {
        //         r[row, column] = Inverse[column + row * Num];
        //     }
        // }
        // Debug.Log(r);
        // inverseBuffer.SetData(Inverse);
        // matrixBuffer.SetData(Matrix);
        // ProcessInverseLine.Dispatch(1, Num, Num, 1);
        // inverseBuffer.GetData(Inverse);
        // matrixBuffer.GetData(Matrix);

        for (var i = 0; i < Num; i++)
        {
            for (var j = 0; j < Num; j++)
            {
                Inverse[j + i * Num] /= Matrix[i + i * Num];
            }
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

    public void ProcessColumnDown(int column)
    {
        if (Matrix[column + column * Num] == 0)
        {
            throw new Exception();
        }

        var distanceToBottom = Num - 1 - column;
        var range = Math.Min(distanceToBottom, Range);
        ProcessInverseLine.SetInt("column", column);
        ProcessInverseLine.SetInt("range", range);

        var buffer= new ComputeBuffer(range, sizeof(double));
        ProcessInverseLine.SetBuffer(3,"buffer",buffer);
        

        // matrixBuffer.SetData(Matrix);
        // inverseBuffer.SetData(Inverse);
        // ProcessInverseLine.Dispatch(3, column + 1, range, 1);
        // matrixBuffer.GetData(Matrix);
        // inverseBuffer.GetData(Inverse);
        for (var i = 1; i <= range; i++)
        {
            if (Matrix[column + (column + i) * Num] == 0)
                continue;
        
            var val = Matrix[column + (column + i) * Num] /
                      Matrix[column + column * Num];
        
            for (var j = 1; j <= range; j++)
            {
                Matrix[column + j + (column + i) * Num] -= Matrix[column + j + column * Num] * val;
            }
        
            ProcessInverseLine.SetInt("i", i);
            ProcessInverseLine.SetFloat("val", (float) val);
            matrixBuffer.SetData(Matrix);
            inverseBuffer.SetData(Inverse);
            ProcessInverseLine.Dispatch(0, column + 1, 1, 1);
            matrixBuffer.GetData(Matrix);
            inverseBuffer.GetData(Inverse);
        
            // for (var j = 0; j < Num; j++)
            // {
            //     Matrix[j + (column + i) * Num] -= Matrix[j + column * Num] * val;
            //
            //     Inverse[j + (column + i) * Num] -= Inverse[j + column * Num] * val;
            // }
        }
    }

    public void ProcessColumnUp(int column)
    {
        if (Matrix[column + column * Num] == 0)
        {
            throw new Exception();
        }

        var distanceToTop = column;
        var range = Math.Min(distanceToTop, Range);
        ProcessInverseLine.SetInt("column", column);

        // inverseBuffer.SetData(Inverse);
        // matrixBuffer.SetData(Matrix);
        // ProcessInverseLine.Dispatch(2, Num, range, 1);
        // matrixBuffer.GetData(Matrix);
        // inverseBuffer.GetData(Inverse);
        for (var i = 1; i <= range; i++)
        {
            if (Matrix[column + (column - i) * Num] == 0)
                continue;

            var val = Matrix[column + (column - i) * Num] / Matrix[column + column * Num];

            ProcessInverseLine.SetInt("i", -i);

            // ProcessInverseLine.SetFloat("val", (float) val);
            // inverseBuffer.SetData(Inverse);
            // matrixBuffer.SetData(Matrix);
            // ProcessInverseLine.Dispatch(0, Num, 1, 1);
            // matrixBuffer.GetData(Matrix);
            // inverseBuffer.GetData(Inverse);


            for (var j = 0; j < Num; j++)
            {
                Inverse[j + (column - i) * Num] -= Inverse[j + column * Num] * val;
            }
        }
    }
}