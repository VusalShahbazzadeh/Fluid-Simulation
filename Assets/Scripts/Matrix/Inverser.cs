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
    private ComputeBuffer valueBuffer;

    public Inverser(double[] matrix, (int x, int y, int z) size)
    {
        ProcessInverseLine = Resources.Load<ComputeShader>("MultiplyAndSubtractInverse");
        Num = size.x * size.y * size.z;
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

        for (var i = 0; i < Num * Num; i++)
        {
            Matrix[i] = matrix[i];
        }

        inverseBuffer = new ComputeBuffer(Num * Num, sizeof(double));
        inverseBuffer.SetData(Inverse);
        matrixBuffer = new ComputeBuffer(Num * Num, sizeof(double));
        matrixBuffer.SetData(Matrix);
        valueBuffer = new ComputeBuffer(Num, sizeof(double));
        
        for (var i = 0; i < 4; i++)
        {
            ProcessInverseLine.SetBuffer(i, "inverse", inverseBuffer);
            ProcessInverseLine.SetBuffer(i, "matrix_buffer", matrixBuffer);
            ProcessInverseLine.SetBuffer(i, "buffer", valueBuffer);
        }
        ProcessInverseLine.SetInt("num", Num);
    }

    public double[] InverseMatrix()
    {
        for (var i = 0; i < Num - 1; i++)
        {
            ProcessColumnDown(i);
        }

        for (var i = Num - 1; i > 0; i--)
        {
            ProcessColumnUp(i);
        }
        
        ProcessInverseLine.Dispatch(0, Num, Num, 1);

        inverseBuffer.GetData(Inverse);

        return Inverse;
    }

    public void ProcessColumnDown(int column)
    {
        var distanceToBottom = Num - 1 - column;
        var range = Math.Min(distanceToBottom, Range);
        ProcessInverseLine.SetInt("column", column);
        ProcessInverseLine.SetInt("range",range);
        ProcessInverseLine.Dispatch(3, range, 1, 1);
        ProcessInverseLine.Dispatch(2, column + 1+range, range, 1);
    }

    public void ProcessColumnUp(int column)
    {
        var distanceToTop = column;
        var range = Math.Min(distanceToTop, Range);
        ProcessInverseLine.SetInt("column", column);
        ProcessInverseLine.Dispatch(3, range, 1, 1);
        ProcessInverseLine.Dispatch(1, Num, range, 1);
    }
}