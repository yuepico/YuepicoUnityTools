using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class VrmBlendShapeBehaviour : PlayableBehaviour
{
    //public UniVRM10.ExpressionKey blendShapePreset = ExpressionPreset.blink;
    [Range(0.0f, 1.0f)]
    public float ExpressionHappy = 0.0f;
    
    [Range(0.0f, 1.0f)]
    public float ExpressionAngry = 0.0f;
    
    [Range(0.0f, 1.0f)]
    public float ExpressionSad = 0.0f;
    
    [Range(0.0f, 1.0f)]
    public float ExpressionRelaxed = 0.0f;
    
    [Range(0.0f, 1.0f)]
    public float ExpressionSurprised = 0.0f;
    
    [TextArea]
    public string clipName = "";
}
