using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    public void UpdateHealth(float current, float max)
    {
        if (healthSlider == null) return;

        healthSlider.maxValue = max;
        healthSlider.value = current;
    }
}