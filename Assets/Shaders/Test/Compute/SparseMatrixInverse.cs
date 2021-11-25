using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SparseMatrixInverse
{
    public Vector3Int size;
    public int num;

    public double[,] mat;
    public double[,] inverse;

    // Start is called before the first frame update
    void Init(Vector3Int _size,double[,] source)
    {
        size = _size;
        mat = new double[num, num];
        
        for (var i = 0; i < num; i++)
        {
            
        }
        
        
        inverse = new double[num, num];

        for (var i = 0; i < num; i++)
        {
            inverse[i, i] = 1;
        }
    }

    void SolveLine(int id)
    {
        var positions = new[]
        {
            1,
            size.x,
            size.x * size.y
        };
        for (var i = 0; i < positions.Length; i++)
        {
            mat[id + positions[i], id] /= mat[id, id];
            inverse[id + positions[i], id] /= inverse[id, id];
            
        }

        mat[id, id] = 1;
        inverse[id, id] = 1;

        for (var i = 0; i < positions.Length; i++)
        {
            var val = mat[id, id + positions[i]];
            var valinverse = inverse[id, id + positions[i]];

            mat[id, id + positions[i]] = 0;
            inverse[id, id + positions[i]] = 0;
            mat[id, id - positions[i]] = 0;
            inverse[id, id - positions[i]] = 0;


            mat[id + 1, id + positions[i] + 1] -= val * mat[id + 1, id];
            inverse[id + 1, id + positions[i] + 1] -= valinverse * inverse[id + 1, id];
            mat[id + 1, id + 1] -= val * mat[id + 1, id];
            inverse[id + 1, id + 1] -= valinverse * inverse[id + 1, id];
            if (i != 0)
            {
                mat[id + 1, id - positions[i] + 1] -= val * mat[id + 1, id];
                inverse[id + 1, id - positions[i] + 1] -= valinverse * inverse[id + 1, id];
            }

            mat[id + size.x, id + size.x + positions[i]] -= val * mat[id + size.x, id];
            inverse[id + size.x, id + size.x + positions[i]] -= valinverse * inverse[id + size.x, id];
            mat[id + size.x, id + size.x] -= val * mat[id + size.x, id];
            inverse[id + size.x, id + size.x] -= valinverse * inverse[id + size.x, id];
            if (i != 1)
            {
                mat[id + size.x, id + size.x - positions[i]] -= val * mat[id + size.x, id];
                inverse[id + size.x, id + size.x - positions[i]] -= valinverse * inverse[id + size.x, id];
            }

            mat[id + size.x * size.y, id + size.x * size.y + positions[i]] -= val * mat[id + size.x * size.y, id];
            inverse[id + size.x * size.y, id + size.x * size.y + positions[i]] -= valinverse * inverse[id + size.x * size.y, id];
            mat[id + size.x * size.y, id + size.x * size.y ] -= val * mat[id + size.x * size.y, id];
            inverse[id + size.x * size.y, id + size.x * size.y ] -= valinverse * inverse[id + size.x * size.y, id];
            if (i!=2)
            {
                mat[id + size.x * size.y, id + size.x * size.y - positions[i]] -= val * mat[id + size.x * size.y, id];
                inverse[id + size.x * size.y, id + size.x * size.y - positions[i]] -= valinverse * inverse[id + size.x * size.y, id];
            }
        }
    }

    public double[,] Inverse()
    {
        for (var i = 0; i < num; i++)
        {
            SolveLine(i);
        }

        return inverse;
    }

    public double[] Solve(double[] rhs)
    {
        return MultiplyToInverse(rhs);
    }

    public double[] MultiplyToInverse(double[] rhs)
    {
        var res = new double[num];
        for (var i = 0; i < num; i++)
        {
            var val = 0d;
            for (var j = 0; j < num; j++)
            {
                val += inverse[j, i] * rhs[j];
            }

            res[i] = val;
        }

        return res;
    }
}