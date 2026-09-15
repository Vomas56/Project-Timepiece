using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomizationUIManager : MonoBehaviour
{
    [SerializeField] private Customization m_bot;
    [SerializeField] private PartSlot m_partslot;
    [SerializeField] private List<GameObject> m_parts;
    [SerializeField] private GameObject m_optionsPanel;
    [SerializeField] private GameObject m_prefab;


    void Update()
    {
        List<GameObject> parts = m_bot.GetParts();
        foreach (GameObject part in parts)
        {
            PartSlot slot = part.GetComponent<Part>().GetPartSlot();
            if (slot == m_partslot && !m_parts.Contains(part))
                {
                    GameObject button = Instantiate(m_prefab, m_optionsPanel.transform);
                    button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = part.GetComponent<Part>().GetName();
                    //Debug.Log(button.transform.GetChild(0).GetComponent<TextMeshProUGUI>() == true);
                    //Debug.Log(part.GetComponent<Part>().name);

                    m_parts.Add(part);

                    int count = m_parts.Count;
                    button.transform.GetComponent<Button>().onClick.AddListener(() => Equip(count - 1));
                }
        }
    }

    public void DisplayOptions()
    {
        m_optionsPanel.SetActive(true);
    }

    public void Equip(int slot = -1)
    {
        if (slot != -1)
        {
            GameObject part = m_parts[slot];
            //this.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = part.GetComponent<Part>().GetName();
            if (m_partslot == PartSlot.Head)
            {
                m_bot.SetHead(part);
            }
            else if (m_partslot == PartSlot.Body)
            {
                m_bot.SetBody(part);
            }
            else if (m_partslot == PartSlot.RArm)
            {
                m_bot.SetRArm(part);
            }
            else if (m_partslot == PartSlot.LArm)
            {
                m_bot.SetLArm(part);
            }
        }

        m_optionsPanel.SetActive(false);
        m_bot.UpdateStats();
    }
}
