using UnityEngine;
using CMF;
public class AnimController : MonoBehaviour
{
    AdvancedWalkerController controller;
    Animator anim;


    void Start()
    {
        controller = GetComponent<AdvancedWalkerController>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        
    }
}
