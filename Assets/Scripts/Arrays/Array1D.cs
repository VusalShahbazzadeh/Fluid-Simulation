using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Array1DInt
{
    private int[] _value;

    public Array1DInt(int count)
    {
        _value = new int[count];
    }

    public int this[int i]
    {
        get=> _value[i];
        set => _value[i] = value;
    }
}


[System.Serializable]
public class Array1DDouble
{
    private double[] _value;

    public Array1DDouble(int count)
    {
        _value = new double[count];
    }

    public double this[int i]
    {
        get=> _value[i];
        set => _value[i] = value;
    }
}
