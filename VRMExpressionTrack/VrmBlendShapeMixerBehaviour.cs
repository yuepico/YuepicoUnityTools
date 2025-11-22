using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;
using UniVRM10;

public class VrmBlendShapeMixerBehaviour : PlayableBehaviour
{
    public TimelineClip[] Clips { get; set; }
    public PlayableDirector Director { get; set; }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var proxy = playerData as UniVRM10.Vrm10Instance;
        if (proxy == null)
        {
            return;
        }

        var time = Director.time;

        // 「喜怒哀楽」
        var value_Happy = 0f;
        var value_Angry = 0f;
        var value_Sad = 0f;
        var value_Relaxed = 0f;
        var value_Surprised = 0f;

        for (int i = 0; i < Clips.Length; i++)
        {
            var clip = Clips[i];
            var clipAsset = clip.asset as VrmBlendShapeClip;
            var behaviour = clipAsset.behaviour;
            var clipWeight = playable.GetInputWeight(i);
            var clipProgress = (float)((time - clip.start) / clip.duration);

            if (clipProgress >= 0.0f && clipProgress <= 1.0f)
            {
                value_Happy += clipWeight * behaviour.ExpressionHappy;
                value_Angry += clipWeight * behaviour.ExpressionAngry;
                value_Sad += clipWeight * behaviour.ExpressionSad;
                value_Relaxed += clipWeight * behaviour.ExpressionRelaxed;
                value_Surprised += clipWeight * behaviour.ExpressionSurprised;
            }
        }

        proxy.Runtime.Expression.SetWeight(
            UniVRM10.ExpressionKey.CreateFromPreset(UniVRM10.ExpressionPreset.happy), value_Happy);
        proxy.Runtime.Expression.SetWeight(
            UniVRM10.ExpressionKey.CreateFromPreset(UniVRM10.ExpressionPreset.angry), value_Angry);
        proxy.Runtime.Expression.SetWeight(
            UniVRM10.ExpressionKey.CreateFromPreset(UniVRM10.ExpressionPreset.sad), value_Sad);
        proxy.Runtime.Expression.SetWeight(
            UniVRM10.ExpressionKey.CreateFromPreset(UniVRM10.ExpressionPreset.relaxed), value_Relaxed);
        proxy.Runtime.Expression.SetWeight(
            UniVRM10.ExpressionKey.CreateFromPreset(UniVRM10.ExpressionPreset.surprised), value_Surprised);
    }
}