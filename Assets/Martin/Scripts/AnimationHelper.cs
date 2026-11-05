using System;
using UnityEngine;
using UnityEngine.Events;

public class AnimationHelper : MonoBehaviour
{
    [SerializeField] private UnityEvent doHit;

    public void DoHit()
    {
        if (doHit != null) doHit.Invoke();
    }
}
