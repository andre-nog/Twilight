using UnityEngine;
using TMPro;

public class QuestTracker : MonoBehaviour
{
    [Header("Configuração da Quest")]
    [SerializeField] private int cowboysDerrotados = 0;
    [SerializeField] private int objetivo = 20;
    [SerializeField] private int recompensaXP = 1000;

    [Header("Referências")]
    [SerializeField] private PlayerXP playerXP;
    [SerializeField] private TextMeshProUGUI questText;

    private bool recompensaEntregue = false;

    public void ContarInimigoDerrotado()
    {
        if (recompensaEntregue) return;

        cowboysDerrotados++;

        if (cowboysDerrotados >= objetivo)
        {
            recompensaEntregue = true;
            playerXP.GainXP(recompensaXP);
            Debug.Log("[QUEST] Recompensa de 1000 XP entregue!");
        }
    }

    void Update()
    {
        if (questText != null)
        {
            string status = recompensaEntregue ? "Concluída!" : $"{cowboysDerrotados}/{objetivo}";
            questText.text = $"<color=#FFDD88><b>Quest:</b></color> Derrote 20 cowboys\n<color=#AAAAAA>Status:</color> {status}";
        }
    }
}