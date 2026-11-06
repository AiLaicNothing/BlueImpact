using UnityEngine;

public class AutomataAnimationEvents : MonoBehaviour
{
    [SerializeField] private Automata automata;

    private void Awake()
    {
        if (automata == null) automata = GetComponentInParent<Automata>();
        if (automata == null) Debug.LogError("[AutomataAnimationEvents] No Automata component found in parents.", this);
    }

    public void ShootBurst()
    {
        if (automata != null) automata.ShootBurst();
    }

    public void ShootSpread()
    {
        if (automata != null) automata.ShootSpread();
    }
}
