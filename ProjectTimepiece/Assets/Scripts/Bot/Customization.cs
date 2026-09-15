using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Customization : MonoBehaviour
{    
    [SerializeField] private GameObject m_head, m_body, m_leftArm, m_rightArm;

    [SerializeField] private List<GameObject> m_knownParts;
    [SerializeField] private List<GameObject> m_allParts;
    
    public void UpdateStats()
    {
        
    }

    public void SetHead(GameObject part)
    {
        Destroy(m_head.transform.GetChild(0).gameObject);
        Instantiate(part, m_head.transform);
    }
    public void SetBody(GameObject part)
    {
        Destroy(m_body.transform.GetChild(0).gameObject);
        Instantiate(part, m_body.transform);
    }
    public void SetLArm(GameObject part)
    {
        Destroy(m_leftArm.transform.GetChild(0).gameObject);
        Instantiate(part, m_leftArm.transform);
    }
    public void SetRArm(GameObject part)
    {
        Destroy(m_rightArm.transform.GetChild(0).gameObject);
        Instantiate(part, m_rightArm.transform);
    }

    public List<GameObject> GetParts()
    {
        return m_knownParts;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Part"))
        {            
            foreach (GameObject part in m_allParts)
            {
                if (other.gameObject.GetComponent<Part>().GetName() == part.GetComponent<Part>().GetName() 
                && other.gameObject.GetComponent<Part>().GetPartSlot() == part.GetComponent<Part>().GetPartSlot())
                {
                    if (!m_knownParts.Contains(part))
                        m_knownParts.Add(part);
                    Destroy(other.gameObject);
                    return;
                }
            }
        }
    }
}

