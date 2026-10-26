using UnityEngine;

[System.Serializable]
public class WorldEvent_State 
{
    public bool completed;
    public int state;

    public WorldEvent_State(bool completed, int state)
    {
        this.completed = completed;
        this.state = state;
    }
}
