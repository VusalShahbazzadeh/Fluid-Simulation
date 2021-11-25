using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleCompute : MonoBehaviour
{
    public ComputeShader ComputeShader;

    public RenderTexture RenderTexture;
    // Start is called before the first frame update
    void Start()
    {
        int kernel = ComputeShader.FindKernel("CSMain");
        RenderTexture.enableRandomWrite = true;
        ComputeShader.SetTexture(kernel,"Result",RenderTexture);
        ComputeShader.Dispatch(kernel,512/8,512/8,1);
        
    }

    // Update is called once per frame
    void Update()
    {
    }
}
