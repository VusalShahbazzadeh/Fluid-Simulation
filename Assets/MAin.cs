using System;
using UnityEngine;
using UnityEngine.UI;

public class MAin : MonoBehaviour
{
    // Start is called before the first frame update
    public LineRenderer line;
    public RenderTexture Texture;
    public MeshRenderer Renderer;
    public int currentFrame = 0;
    private DynamicReservoirModel model;
    public float YTile, XTile;

    public ComputeShader Shader;
    private int kernel;

    private DateTime time;
    private ComputeBuffer buffer;

    void Start()
    {
        (int x, int y) Size = (4, 4);
        var reservoir = new HomogenousReservoir(0.2f, new[] {100d, 100d, 100d});
        var fluids = new[]
        {
            new Fluid(1, 0.00001d)
        };
        var initialPressure = new double[Size.x * Size.y];
        for (var i = 0; i < Size.x * Size.y; i++)
        {
            initialPressure[i] = 1000;
        }

        model = new DynamicReservoirModel(
            (10000, 10000, 1),
            (Size.x, Size.y, 1),
            reservoir,
            fluids,
            0.1d,
            0.01d,
            initialPressure
        );

        model.ImplementBorderConditions(0, new[] {1d, 0, 1000}, new[] {1d, 0, 1000});
        model.ImplementBorderConditions(1, new[] {1d, 0, 1000}, new[] {1d, 0, 1000});

        time = DateTime.Now;
        model.ImplementWell(new[] {8}, new[] {-100d}, new[] {0d});
        model.Solve();
        Debug.Log((DateTime.Now - time).TotalSeconds);
        Texture = new RenderTexture(model.Grid.GridNum[0], model.Grid.GridNum[1], 24);
        Texture.filterMode = FilterMode.Point;
        Renderer.material = new Material(UnityEngine.Shader.Find("Standard"));
        Renderer.material.mainTexture = Texture;
        Texture.enableRandomWrite = true;
        lastTime = Time.time;

        int kernel = Shader.FindKernel("CSMain");
        Shader.SetTexture(kernel, "Result", Texture);
        Shader.SetInt("size_x", model.Grid.GridNum[0]);
        line.positionCount = Size.x * Size.y;
    }

    // Update is called once per frame
    private float lastTime;

    void Update()
    {
        if (Time.time <= lastTime + 0.2f)
            return;
        var frame = currentFrame % model.FrameCount;
        frame += frame < 0 ? model.FrameCount : 0;
        buffer = new ComputeBuffer(model.Grid.Num, sizeof(double));
        var result = new double[model.Grid.Num];
        var min = double.MaxValue;
        var max = 0d;
        for (var i = 0; i < model.Grid.Num; i++)
        {
            var val = model.Pressure[i, frame];
            min = Math.Min(val, min);
            max = Math.Max(val, max);
        }

        for (var i = 0; i < model.Grid.Num; i++)
        {
            result[i] = (model.Pressure[i, frame] - min)/ (max-min);
            Vector3 pos = new Vector3(i * XTile, (float) model.Pressure[i, frame] * YTile, 0);
            line.SetPosition(i, pos);
        }

        buffer.SetData(result);
        Shader.SetBuffer(kernel, "dataBuffer", buffer);
        Shader.Dispatch(kernel, model.Grid.GridNum[0], model.Grid.GridNum[1], 1);
        buffer.Dispose();

        currentFrame++;
        lastTime = Time.time;
    }
}