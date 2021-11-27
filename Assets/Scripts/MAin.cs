using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MAin : MonoBehaviour
{
    // Start is called before the first frame update
    public RenderTexture Texture;
    public MeshRenderer Renderer;
    public int currentFrame = 0;
    public DynamicReservoirModel model;
    public float YTile, XTile;

    public ComputeShader Shader;
    private int kernel;

    private DateTime time;
    private ComputeBuffer buffer;

    public List<double[]> Pressures;

    void Start()
    {
        (int x, int y) Size = (4, 4);
        Texture = new RenderTexture(model.Grid.GridNum[0], model.Grid.GridNum[1], 24);
        Texture.filterMode = FilterMode.Point;
        Renderer.material = new Material(UnityEngine.Shader.Find("Standard"));
        Renderer.material.mainTexture = Texture;
        Texture.enableRandomWrite = true;
        lastTime = Time.time;

        int kernel = Shader.FindKernel("CSMain");
        Shader.SetTexture(kernel, "Result", Texture);
        Shader.SetInt("size_x", model.Grid.GridNum[0]);
        var initial = new double[model.Grid.Num];
        for (var i = 0; i < model.Grid.Num; i++)
        {
            initial[i] = 1000;
        }

        Pressures = new List<double[]>
        {
            initial
        };
    }

    // Update is called once per frame
    private float lastTime;

    //
    void Update()
    {
        if (Time.time <= lastTime + 0.2f)
            return;
        buffer = new ComputeBuffer(model.Grid.Num, sizeof(double));
        var result = new double[model.Grid.Num];
        var min = double.MaxValue;
        var max = 0d;
        for (var i = 0; i < model.Grid.Num; i++)
        {
            var val = Pressures[currentFrame][i];
            min = Math.Min(val, min);
            max = Math.Max(val, max);
        }

        for (var i = 0; i < model.Grid.Num; i++)
        {
            result[i] = (Pressures[currentFrame][i] - min) / (max - min);
            Vector3 pos = new Vector3(i * XTile, (float) Pressures[currentFrame][i] * YTile, 0);
        }

        buffer.SetData(result);
        Shader.SetBuffer(kernel, "dataBuffer", buffer);
        Shader.Dispatch(kernel, model.Grid.GridNum[0], model.Grid.GridNum[1], 1);
        buffer.Dispose();

        Pressures.Add(model.Solve(Pressures[currentFrame]));
        currentFrame++;
        lastTime = Time.time;
    }
}