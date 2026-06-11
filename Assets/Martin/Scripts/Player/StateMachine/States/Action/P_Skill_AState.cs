using UnityEngine;

public class P_Skill_AState : PlayerState
{
    public P_Skill_AState(PlayerController player) : base(player) { }

    Skill currentSkill;
    int skillIndex;

    float timer;
    bool isCasting = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetSkill(Skill desiredSkill, int index)
    {
        currentSkill = desiredSkill;
        skillIndex = index;
    }
}
