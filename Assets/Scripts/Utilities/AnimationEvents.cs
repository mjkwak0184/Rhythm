using System;
using UnityEngine;

public class AnimationEvents: MonoBehaviour
{

    public Action callback;
    public void DisableObject()
    {
        gameObject.SetActive(false);
    }

    public void Callback()
    {
        if(callback != null) callback();
    }
}