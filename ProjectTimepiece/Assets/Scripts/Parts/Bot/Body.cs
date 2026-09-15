using UnityEngine;

public class Body : Part
{
    private void Awake()
    {
        base.Awake();
        m_slot = PartSlot.Body;
    }
}
