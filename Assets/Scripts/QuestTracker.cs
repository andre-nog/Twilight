using UnityEngine;
using TMPro;

public class QuestTracker : MonoBehaviour
{
    [Header("Quest 1 - Derrote 20 cowboys")]
    [SerializeField] private int cowboysDerrotados = 0;
    [SerializeField] private int objetivoCowboys = 20;
    [SerializeField] private int recompensaXPCowboys = 1000;
    private bool recompensaCowboysEntregue = false;

    [Header("Quest 2 - Derrote o boss final")]
    [SerializeField] private int recompensaXPBoss = 1000;
    private bool recompensaBossEntregue = false;

    [Header("Referências")]
    [SerializeField] private PlayerXP playerXP;
    [SerializeField] private TextMeshProUGUI questText;

    // chamada ao derrotar um cowboy
    public void ContarInimigoDerrotado()
    {
        if (recompensaCowboysEntregue) return;

        cowboysDerrotados++;

        if (cowboysDerrotados >= objetivoCowboys)
        {
            recompensaCowboysEntregue = true;
            playerXP.GainXP(recompensaXPCowboys);
            Debug.Log("[QUEST] Recompensa por derrotar 20 cowboys entregue!");
        }
    }

    // chamada ao derrotar o boss
    public void BossFinalDerrotado()
    {
        if (recompensaBossEntregue) return;

        recompensaBossEntregue = true;
        playerXP.GainXP(recompensaXPBoss);
        Debug.Log("[QUEST] Recompensa por derrotar o boss final entregue!");
    }

    void Update()
    {
        if (questText != null)
        {
            string status1 = recompensaCowboysEntregue ? "Concluída!" : $"{cowboysDerrotados}/{objetivoCowboys}";
            string status2 = recompensaBossEntregue ? "Concluída!" : "Pendente";

            questText.text = $@"
<color=#FFDD88><b>Quest 1:</b></color> Derrote 20 cowboys
<color=#AAAAAA>Status:</color> {status1}

<color=#FFDD88><b>Quest 2:</b></color> Derrote o boss final
<color=#AAAAAA>Status:</color> {status2}";
        }
    }
}