using UnityEngine;

public enum PartSlot {Head, Body, RArm, LArm}

public class Part : MonoBehaviour
{
    // Part stats
    [SerializeField] protected string m_name;
    [SerializeField] protected PartSlot m_slot;
    [SerializeField] protected int m_damage;
    [SerializeField] protected float m_attackSpeed;
    [SerializeField] protected int m_energyCost;
    [SerializeField] protected float m_speed;
    [SerializeField] protected int m_health;
    [SerializeField] protected float m_dashSpeed;

    protected void Awake()
    {
        //m_name = gameObject.name;
    }

    // Getters
    public string GetName()
    {
        return m_name;
    }
    public PartSlot GetPartSlot()
    {
        return m_slot;
    }
        
}
