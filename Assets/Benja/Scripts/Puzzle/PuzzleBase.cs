using UnityEngine;

public abstract class PuzzleBase : MonoBehaviour, IActivatable
{
    public virtual void Activate() { return; }

    public virtual void Deactivate() { return; }

}
