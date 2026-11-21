using UnityEngine;

public class PuzzleAmbushStarter : PuzzleBase
{
    private bool _isOpen;
    [SerializeField]Collider ambushEvent;
    private void Start()
    {
        ambushEvent.enabled = false;
    }

    void ActivateAmbush()
    {
        ambushEvent.enabled = true;
    }
    public override void Activate()
    {
        if (_isOpen) return;

        _isOpen = true;

        ActivateAmbush();
    }

    public override void Deactivate()
    {
        if (!_isOpen) return;

        _isOpen = false;

    }
}
