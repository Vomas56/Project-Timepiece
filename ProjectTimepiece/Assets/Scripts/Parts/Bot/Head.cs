using UnityEngine;

public class Head : Part
{
    private void Awake()
    {
        base.Awake();
        m_slot = PartSlot.Head;
    }
}
